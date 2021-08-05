namespace TtvAnalytics.Control {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     Visualizes a twitch account.
    /// </summary>
    public class TwitchAccountControl : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchAccountControl" /> class.
        /// </summary>
        public TwitchAccountControl() {
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