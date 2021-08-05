namespace TtvAnalytics.ViewModel {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using log4net;
    using ReactiveUI;
    using Utilities;

    /// <summary>
    ///     The view model for performing Ad Hoc searches for twitch viewers.
    /// </summary>
    internal class TwitchChatViewerListViewModel : ViewModelBase {
        /// <summary>
        ///     The logger.
        /// </summary>
        private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchChatViewerListViewModel));

        /// <summary>
        ///     A value indicating whether the query is currently running.
        /// </summary>
        private bool isRunning;

        /// <summary>
        ///     The value between 0-100 for how far into the query we are.
        /// </summary>
        private int queryProgress;

        /// <summary>
        ///     The twitch channel to search for.
        /// </summary>
        private string? twitchChannel;

        /// <summary>
        ///     The list of chatters.
        /// </summary>
        private IEnumerable<string>? twitchChatters;

        /// <summary>
        ///     Gets or sets the twitch channel to search for.
        /// </summary>
        public string? TwitchChannel {
            get => this.twitchChannel;
            set => this.RaiseAndSetIfChanged(ref this.twitchChannel, value);
        }

        /// <summary>
        ///     Gets or sets the list of chatters.
        /// </summary>
        public IEnumerable<string>? TwitchChatters {
            get => this.twitchChatters;
            set => this.RaiseAndSetIfChanged(ref this.twitchChatters, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the query is currently running.
        /// </summary>
        public bool IsRunning {
            get => this.isRunning;
            set => this.RaiseAndSetIfChanged(ref this.isRunning, value);
        }

        /// <summary>
        ///     Gets or sets the value between 0-100 for how far into the query we are.
        /// </summary>
        public int QueryProgress {
            get => this.queryProgress;
            set => this.RaiseAndSetIfChanged(ref this.queryProgress, value);
        }

        /// <summary>
        ///     Retrieves the list of users in a channel's chat.
        /// </summary>
        public async void GetChannelsUserIsIn() {
            // Disable everything while we do the query.
            this.IsRunning = true;
            try {
                if (null == this.TwitchChannel) {
                    return;
                }

                // Get the list of channels that the user followers.
                TwitchClient client = new TwitchClient();
                var peopleUserIsFollowingE = await client.GetChannelFollows(this.TwitchChannel);
                if (null == peopleUserIsFollowingE) {
                    return;
                }

                var peopleUserIsFollowing = peopleUserIsFollowingE.ToArray();

                // Get the list of channels they follow that are currently live.
                var liveChannelsE = await client.GetLiveChannels(peopleUserIsFollowing.Select(f => f.ToUserId).ToArray());
                if (null == liveChannelsE) {
                    return;
                }

                var liveChannels = liveChannelsE.ToArray();

                // Go through each channel and determine if they're in there.
                var inChannels = new List<string>();
                foreach (var channel in liveChannels) {
                    var users = await client.GetTwitchChatters(channel.UserLogin);
                    if (null == users) {
                        continue;
                    }

                    users = users.Select(u => u.ToLowerInvariant()).ToArray();
                    if (users.Contains(this.TwitchChannel.ToLowerInvariant())) {
                        inChannels.Add(channel.UserLogin);
                    }
                }

                inChannels.Insert(0, $"Following: {peopleUserIsFollowing.Length} Live Following: {liveChannels.Length} In Chats: {inChannels.Count}");
                this.TwitchChatters = inChannels.ToArray();
            } finally {
                this.IsRunning = false;
            }
        }

        /// <summary>
        ///     Retrieves the list of all followers of a channel and what chats they're currently in.
        /// </summary>
        public async void GetChannelsAllFollowersAreIn() {
            this.IsRunning = true;
            this.QueryProgress = 0;
            var watch = new Stopwatch();
            try {
                if (null == this.TwitchChannel) {
                    return;
                }

                TwitchClient client = new TwitchClient();

                // Step 1: Get your followers
                watch.Start();
                var peopleFollowingThisUserE = await client.GetChannelFollows(usernameBeingFollowed: this.TwitchChannel);
                if (null == peopleFollowingThisUserE) {
                    return;
                }

                var peopleFollowingThisUser = peopleFollowingThisUserE.ToArray();

                watch.Stop();
                TwitchChatViewerListViewModel.LOG.Debug($"Get your followers: {watch.ElapsedMilliseconds}");

                // Step 2: Get who they follow (Second Longest)
                this.QueryProgress = 25;
                watch.Restart();
                watch.Start();
                HashSet<string> followingUserIds = new HashSet<string>();
                for (var i = 0; i < peopleFollowingThisUser.Length; i++) {
                    var follower = peopleFollowingThisUser[i];
                    var following = await client.GetChannelFollows(follower.FromUserName);
                    if (null == following) {
                        continue;
                    }

                    following.Select(f => followingUserIds.Add(f.ToUserId)).ToArray();
                    this.QueryProgress = 25 + (int)Math.Ceiling(i / (double)peopleFollowingThisUser.Length * 25.0);
                }

                watch.Stop();
                TwitchChatViewerListViewModel.LOG.Debug($"Get who they follow: {watch.ElapsedMilliseconds}");

                // Step 3: Get who they follow that is live
                this.QueryProgress = 50;
                watch.Restart();
                watch.Start();
                var liveChannelsE = await client.GetLiveChannels(followingUserIds.ToArray());
                if (null == liveChannelsE) {
                    return;
                }

                var liveChannels = liveChannelsE.ToArray();

                watch.Stop();
                TwitchChatViewerListViewModel.LOG.Debug($"Get who they follow that is live: {watch.ElapsedMilliseconds}");

                // Step 4: Find out if they're in their chats (Longest)
                this.QueryProgress = 75;
                watch.Restart();
                watch.Start();
                var inChannels = new List<string>();
                var peopleFollowingThisUserLower = peopleFollowingThisUser.Select(p => p.FromUserName.ToLowerInvariant()).ToArray();
                for (var i = 0; i < liveChannels.Length; i++) {
                    var channel = liveChannels[i];
                    var usersInChat = await client.GetTwitchChatters(channel.UserLogin);
                    if (null == usersInChat) {
                        continue;
                    }

                    usersInChat = usersInChat.Select(u => u.ToLowerInvariant()).ToArray();
                    inChannels.AddRange(usersInChat.Where(u => peopleFollowingThisUserLower.Contains(u)).Select(u => $"{u} -> {channel.UserLogin}"));
                    this.QueryProgress = 75 + (int)Math.Ceiling(i / (double)liveChannels.Length * 25.0);
                }

                watch.Stop();
                TwitchChatViewerListViewModel.LOG.Debug($"Find out if they're in their chats: {watch.ElapsedMilliseconds}");

                this.TwitchChatters = inChannels.ToArray();
                this.QueryProgress = 100;
            } finally {
                this.IsRunning = false;
            }
        }
    }
}