namespace TtvAnalytics.Control {
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     Visualizes a twitch account.
    /// </summary>
    public class DatabaseConfigurationControl : UserControl {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseConfigurationControl" /> class.
        /// </summary>
        public DatabaseConfigurationControl() {
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