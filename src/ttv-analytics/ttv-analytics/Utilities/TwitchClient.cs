namespace TtvAnalytics.Utilities {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using log4net;
    using Newtonsoft.Json;
    using ttv_analytics.Model;
    using TwitchLib.Api;
    using TwitchLib.Api.Core.Enums;
    using TwitchLib.Api.Helix.Models.Streams.GetStreams;
    using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
    using TwitchLib.Api.Helix.Models.Users.GetUsers;

    /// <summary>
    ///     Wrappers the functionality of interacting with the Twitch API.
    /// </summary>
    internal class TwitchClient {
        /// <summary>
        ///     The logger.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(TwitchClient));

        /// <summary>
        ///     A cached instance of the API to use between calls.
        /// </summary>
        private TwitchAPI? api;

        /// <summary>
        ///     Gets or creates a new API object.
        /// </summary>
        /// <returns>The <see cref="TwitchAPI" /> object if successful, null otherwise.</returns>
        public async Task<TwitchAPI?> GetTwitchClientApi() {
            // If we have no settings, there is nothing we can do.
            var oauthToken = Configuration.Instance.OAuth;
            if (null == oauthToken) {
                return null;
            }

            // If the token has not yet expired, use the API we already have.
            if (null != this.api && oauthToken.TokenExpiration > DateTime.UtcNow) {
                return this.api;
            }

            // Create a new API object.
            var api = new TwitchAPI();
            api.Settings.ClientId = Constants.TWITCH_CLIENT_ID;
            api.Settings.Scopes = new List<AuthScopes>();
            api.Settings.Scopes.Add(AuthScopes.Channel_Read);

            // If the token is expired, we need to refresh it.
            if (oauthToken.TokenExpiration <= DateTime.UtcNow) {
                try {
                    // Reach out to twitch and tell them we want a new token.
                    var client = new HttpClient();
                    var nullinsideResponse = await client.PostAsync($"{Constants.TWITCH_REFRESH}?refresh_token={oauthToken.RefreshToken}", new StringContent(""));
                    if (!nullinsideResponse.IsSuccessStatusCode) {
                        return null;
                    }

                    var responseString = await nullinsideResponse.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<SpringOAuthRefreshResponse>(responseString);
                    if (null == json) {
                        return api;
                    }

                    // Update the token in our configuration file.
                    Configuration.Instance.OAuth = new Configuration.OAuthToken(json.access_token, json.refresh_token, DateTime.UtcNow + new TimeSpan(0, 0, json.expires_in - 300));
                } catch (Exception e) {
                    TwitchClient.log.Error("Failed to generate refresh token", e);
                }
            }

            // Save the most up-to-date token in the API.
#pragma warning disable 8602
            api.Settings.AccessToken = Configuration.Instance.OAuth.Token;
#pragma warning restore 8602
            this.api = api;
            return api;
        }

        /// <summary>
        ///     Retrieve the list of people joined to a channel's chat.
        /// </summary>
        /// <param name="channel">The username of the channel.</param>
        /// <returns>The collection of twitch usernames that are in a chat if successful, null otherwise.</returns>
        public async Task<IEnumerable<string>?> GetTwitchChatters(string channel) {
            var api = await this.GetTwitchClientApi();
            if (null == api) {
                return null;
            }

            try {
                var chatters = await api.Undocumented.GetChattersAsync(channel);
                return chatters.Select(c => c.Username).ToArray();
            } catch (Exception e) {
                TwitchClient.log.Error($"Failed to get twitch chatters for: {channel}", e);
                return null;
            }
        }

        /// <summary>
        ///     Retrieves either the people following a user or the people a user is following.
        /// </summary>
        /// <param name="usernameFollowing">If specified, the username to find who they are following.</param>
        /// <param name="usernameBeingFollowed">If specified, the username to find who is following them.</param>
        /// <remarks>
        ///     You must specify either the <paramref name="usernameFollowing"/> or the <paramref name="usernameBeingFollowed" />
        ///     but not both.
        /// </remarks>
        /// <returns>A collection of users if successful, null otherwise.</returns>
        public async Task<IEnumerable<Follow>?> GetChannelFollows(string? usernameFollowing = null, string? usernameBeingFollowed = null) {
            // Sanity checks.
            if (string.IsNullOrWhiteSpace(usernameFollowing) && string.IsNullOrWhiteSpace(usernameBeingFollowed)) {
                throw new ArgumentException($"Need to specify either {nameof(usernameFollowing)} or {nameof(usernameBeingFollowed)}");
            }

            if (!string.IsNullOrWhiteSpace(usernameFollowing) && !string.IsNullOrWhiteSpace(usernameBeingFollowed)) {
                throw new ArgumentException($"Need to specify either {nameof(usernameFollowing)} or {nameof(usernameBeingFollowed)}, not both");
            }

            var api = await this.GetTwitchClientApi();
            if (null == api) {
                return null;
            }

            // Get the user information so we can map the username to the unique identifier from twitch.
            var username = string.IsNullOrWhiteSpace(usernameFollowing) ? usernameBeingFollowed : usernameFollowing;
            if (string.IsNullOrWhiteSpace(username)) {
                return null;
            }

            GetUsersResponse? userInfo = null;
            try {
                userInfo = await api.Helix.Users.GetUsersAsync(logins: new List<string> { username });
            } catch (Exception e) {
                TwitchClient.log.Error($"Failed to get user information for: {username}", e);
            }

            if (null == userInfo || userInfo.Users.Length == 0) {
                return null;
            }

            // We can only query 100 followers at a time. In a loop, we will grab each page of followers until we get them all.
            var following = new List<Follow>();
            string? cursor = null;
            while (true) {
                GetUsersFollowsResponse? userFollows = null;
                try {
                    if (!string.IsNullOrWhiteSpace(usernameFollowing)) {
                        userFollows = await api.Helix.Users.GetUsersFollowsAsync(fromId: userInfo.Users[0].Id, first: 100, after: cursor);
                    } else {
                        userFollows = await api.Helix.Users.GetUsersFollowsAsync(toId: userInfo.Users[0].Id, first: 100, after: cursor);
                    }
                } catch (Exception e) {
                    TwitchClient.log.Error($"Failed to get followers of: {username}", e);
                }

                if (null == userFollows || userFollows.Follows.Length == 0) {
                    break;
                }

                following.AddRange(userFollows.Follows);

                // If we had a cursor (used for pagination) and we have no longer do, it means this was the last page of results.
                // Likewise, If we didn't have a cursor and we still dont' have one, it means there was only one page of results.
                // In both cases, we're done.
                var thisIsTheLastPage = !string.IsNullOrWhiteSpace(cursor) && string.IsNullOrWhiteSpace(userFollows.Pagination.Cursor);
                var thisIsTheOnlyPage = string.IsNullOrWhiteSpace(cursor) && string.IsNullOrWhiteSpace(userFollows.Pagination.Cursor);
                if (thisIsTheLastPage || thisIsTheOnlyPage) {
                    break;
                }

                cursor = userFollows.Pagination.Cursor;
            }

            return following;
        }

        /// <summary>
        ///     Determine which channels from a collection of channels is currently live.
        /// </summary>
        /// <param name="channelIds">The channel ids to check.</param>
        /// <returns>The collection of live streams, if successful, null otherwise.</returns>
        public async Task<IEnumerable<Stream>?> GetLiveChannels(string[] channelIds) {
            const int perPage = 100;

            var api = await this.GetTwitchClientApi();
            if (null == api) {
                return null;
            }

            var allLiveUsers = new List<Stream>();
            var index = 0;
            while (index < channelIds.Length) {
                var query = new List<string>();
                for (var i = index; i < index + perPage && i < channelIds.Length; i++) {
                    query.Add(channelIds[i]);
                }

                index += perPage;

                try {
                    var liveUsers = await api.Helix.Streams.GetStreamsAsync(first: query.Count, userIds: query, type: "live");
                    allLiveUsers.AddRange(liveUsers.Streams);
                } catch (Exception e) {
                    TwitchClient.log.Error($"Failed to get list of live channels from: {query}", e);
                }
            }

            return allLiveUsers;
        }
    }
}