namespace TtvAnalytics.Control {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     Visualizes the list of twitch chat viewers.
    /// </summary>
    public class TwitchChatViewerListControl : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatViewerListControl" /> class.
        /// </summary>
        public TwitchChatViewerListControl() {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Initializes the GUI components.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}