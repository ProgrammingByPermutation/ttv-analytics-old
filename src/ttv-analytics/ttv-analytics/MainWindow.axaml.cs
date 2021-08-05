namespace TtvAnalytics {
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    ///     The main application window.
    /// </summary>
    public class MainWindow : Window {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow() {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        /// <summary>
        ///     Initializes the GUI.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}