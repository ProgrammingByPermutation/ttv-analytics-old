namespace TtvAnalytics {
    using Avalonia;
    using Avalonia.Controls;

    /// <summary>
    ///     The main entry point of the application.
    /// </summary>
    internal class Program {
        /// <summary>
        ///     Initialization code. Don't use any Avalonia, third-party APIs or any
        ///     SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        ///     yet and stuff might break.
        /// </summary>
        /// <param name="args">The args passed to the program.</param>
        public static void Main(string[] args) {
            Program.BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        ///     Avalonia configuration, don't remove; also used by visual designer.
        /// </summary>
        /// <returns>The Avalonia application.</returns>
        public static AppBuilder BuildAvaloniaApp() {
            return AppBuilderBase<AppBuilder>.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}