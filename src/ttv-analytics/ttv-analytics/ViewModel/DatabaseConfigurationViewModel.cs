namespace TtvAnalytics.ViewModel {
    using System.ComponentModel;
    using ReactiveUI;

    /// <summary>
    ///     The view model tracking and updating the.
    /// </summary>
    internal class DatabaseConfigurationViewModel : ViewModelBase {
        private string database;
        private string password;
        private string server;
        private string username;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseConfigurationViewModel" /> class.
        /// </summary>
        public DatabaseConfigurationViewModel() {
            // Load the configuration from the life.
            var config = Configuration.Instance.DatabaseConfig;
            if (null != config) {
                this.Username = config.Username;
                this.Password = config.Password;
                this.Database = config.Database;
                this.Server = config.Server;
            }

            // Ensure that we write changes to the GUI to the configuration file.
            this.PropertyChanged += this.OnPropertyChanged;
        }

        public string Server {
            get => this.server;
            set => this.RaiseAndSetIfChanged(ref this.server, value);
        }

        public string Username {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        public string Password {
            get => this.password;
            set => this.RaiseAndSetIfChanged(ref this.password, value);
        }

        public string Database {
            get => this.database;
            set => this.RaiseAndSetIfChanged(ref this.database, value);
        }

        /// <summary>
        ///     Handles updating the configuration file with the changes in the GUI.
        /// </summary>
        /// <param name="sender">This object instance.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            Configuration.Instance.DatabaseConfig = new Configuration.DatabaseConfiguration(this.Server, this.Username, this.Password, this.Database);
        }
    }
}