namespace TtvAnalytics.ViewModel {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Timers;
    using Newtonsoft.Json;
    using ReactiveUI;
    using ttv_analytics.Model;

    /// <summary>
    ///     The view model tracking and updating the.
    /// </summary>
    internal class TwitchAccountViewModel : ViewModelBase {
        /// <summary>
        ///     The timer that looks for the copied OAuth token on the clipboard.
        /// </summary>
        private readonly Timer oauthCheckTimer;

        /// <summary>
        ///     The JSON representation of the OAuth token. <seealso cref="Configuration.OAuthToken" />
        /// </summary>
        private string? oauth;

        /// <summary>
        ///     The twitch username to track.
        /// </summary>
        private string? username;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchAccountViewModel" /> class.
        /// </summary>
        public TwitchAccountViewModel() {
            this.oauthCheckTimer = new Timer(100) { AutoReset = false };
            this.oauthCheckTimer.Elapsed += this.OauthCodeCheckTimer_Elapsed;

            // Load the configuration from the life.
            this.Username = Configuration.Instance.TwitchUsername;
            if (null != Configuration.Instance.OAuth && !string.IsNullOrWhiteSpace(Configuration.Instance.OAuth.Token)) {
                this.OAuth = JsonConvert.SerializeObject(Configuration.Instance.OAuth);
            }

            // Ensure that we write changes to the GUI to the configuration file.
            this.PropertyChanged += this.OnPropertyChanged;
        }

        /// <summary>
        ///     Gets or sets the twitch username to track.
        /// </summary>
        public string? Username {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        /// <summary>
        ///     Gets or sets the JSON representation of the OAuth token. <seealso cref="Configuration.OAuthToken" />
        /// </summary>
        public string? OAuth {
            get => this.oauth;
            set => this.RaiseAndSetIfChanged(ref this.oauth, value);
        }

        /// <summary>
        ///     Retrieves an OAuth token from twitch.
        /// </summary>
        public async void GetOAuthToken() {
            if (null == Constants.CLIPBOARD) {
                return;
            }

            string url = $"https://id.twitch.tv/oauth2/authorize?client_id={Constants.TWITCH_CLIENT_ID}&" +
                         $"redirect_uri={Constants.TWITCH_REDIRECT_URL}&" +
                         "response_type=code&" +
                         $"scope={string.Join("%20", Constants.TWITCH_SCOPES)}";

            await Constants.CLIPBOARD.ClearAsync();
            this.oauthCheckTimer.Start();
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true });
        }

        /// <summary>
        ///     Periodically checks to see if the <seealso cref="SpringOAuthResponse" /> JSON is on the clipboard
        ///     from the OAuth website.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private async void OauthCodeCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (null == Constants.CLIPBOARD) {
                return;
            }

            var text = await Constants.CLIPBOARD.GetTextAsync();

            try {
                SpringOAuthResponse oAuthResponse = JsonConvert.DeserializeObject<SpringOAuthResponse>(text);
                Configuration.OAuthToken token = new Configuration.OAuthToken(oAuthResponse.token, oAuthResponse.refresh_token,
                    DateTime.UtcNow + new TimeSpan(0, 0, oAuthResponse.expires_in - 300));

                this.OAuth = JsonConvert.SerializeObject(token);
            } catch (Exception) {
                // If what was on the clipboard was not the JSON, then restart.
                this.oauthCheckTimer.Start();
            }
        }

        /// <summary>
        ///     Handles updating the configuration file with the changes in the GUI.
        /// </summary>
        /// <param name="sender">This object instance.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (nameof(TwitchAccountViewModel.Username).Equals(e.PropertyName)) {
                Configuration.Instance.TwitchUsername = this.Username;
            } else if (nameof(TwitchAccountViewModel.OAuth).Equals(e.PropertyName)) {
                if (string.IsNullOrWhiteSpace(this.OAuth)) {
                    return;
                }

                Configuration.Instance.OAuth = JsonConvert.DeserializeObject<Configuration.OAuthToken>(this.OAuth);
            }
        }
    }
}