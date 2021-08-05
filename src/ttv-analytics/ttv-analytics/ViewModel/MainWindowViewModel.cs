namespace TtvAnalytics.ViewModel {
    /// <summary>
    ///     The view model for the main application page.
    /// </summary>
    internal class MainWindowViewModel : ViewModelBase {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
        /// </summary>
        public MainWindowViewModel() {
            this.TwitchAccountViewModel = new TwitchAccountViewModel();
            this.TwitchChatViewerListViewModel = new TwitchChatViewerListViewModel();
        }

        /// <summary>
        ///     Gets or sets the view model for the twitch accounts.
        /// </summary>
        public TwitchAccountViewModel TwitchAccountViewModel { get; set; }

        /// <summary>
        ///     Gets or sets the view model for the twitch chat viewer.
        /// </summary>
        public TwitchChatViewerListViewModel TwitchChatViewerListViewModel { get; set; }
    }
}