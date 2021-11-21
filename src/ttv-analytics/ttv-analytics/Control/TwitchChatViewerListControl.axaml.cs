namespace TtvAnalytics.Control {
    using System.Diagnostics;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
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

        private void OpenBrowser(object? sender, RoutedEventArgs e) {
            ListBox? control = sender as ListBox;
            if (null == control || string.IsNullOrWhiteSpace(control.SelectedItem?.ToString()) || (control.SelectedItem.ToString()?.Contains(":") ?? true)) {
                return;
            }

            Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.twitch.tv/{control.SelectedItem}") { CreateNoWindow = true });
        }
    }
}