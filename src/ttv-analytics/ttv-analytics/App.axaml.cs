namespace TtvAnalytics {
    using System.IO;
    using System.Reflection;
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using log4net;
    using log4net.Config;
    using ViewModel;

    /// <summary>
    ///     The main entry point of the application.
    /// </summary>
    public class App : Application {
        /// <summary>
        ///     The logger.
        /// </summary>
        private static readonly ILog LOG = LogManager.GetLogger(typeof(App));

        /// <summary>
        ///     The main initialization of the application.
        /// </summary>
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
            Constants.CLIPBOARD = this.Clipboard;
        }

        /// <summary>
        ///     The initialization of the GUI after the application has initialized.
        /// </summary>
        public override void OnFrameworkInitializationCompleted() {
            // Initializes the log4net framework.
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            // Log that we started the application so we can keep track of runs.
            App.LOG.Info("Application start");

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.DataContext = new MainWindowViewModel();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}