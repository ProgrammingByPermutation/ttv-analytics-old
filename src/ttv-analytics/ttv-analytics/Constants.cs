namespace TtvAnalytics {
    using Avalonia.Input.Platform;

    /// <summary>
    ///     The constant values used throughout the application.
    /// </summary>
    internal static class Constants {
        /// <summary>
        ///     The twitch client id from the developer console of the twitch website.
        /// </summary>
        public const string TWITCH_CLIENT_ID = "n0k3wfvgqk66nlnmeudixjtaajj9xc";

        /// <summary>
        ///     The URL to redirect to when asking twitch for an OAuth token.
        /// </summary>
        public const string TWITCH_REDIRECT_URL = @"https://www.nullinside.com/react/twitch_oauth/ttv_analytics";

        /// <summary>
        ///     The endpoint the refreshes an OAuth token.
        /// </summary>
        public const string TWITCH_REFRESH = @"https://www.nullinside.com/api/v1/twitch/ttv_analytics/oauth/refresh";

        /// <summary>
        ///     The scopes requested when generating an OAuth token.
        /// </summary>
        public static readonly string[] TWITCH_SCOPES = { "chat:read" };

        /// <summary>
        ///     The reference to the clipboard API.
        /// </summary>
        /// <remarks>This is a hack because it's hard to get to.</remarks>
        public static IClipboard? CLIPBOARD;
    }
}