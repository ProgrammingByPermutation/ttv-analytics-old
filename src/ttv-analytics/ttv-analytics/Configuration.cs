namespace TtvAnalytics {
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using JetBrains.Annotations;
    using log4net;
    using Newtonsoft.Json;

    /// <summary>
    ///     The persistent configuration.
    /// </summary>
    public class Configuration : INotifyPropertyChanged {
        /// <summary>
        ///     The logger.
        /// </summary>
        private static readonly ILog LOG = LogManager.GetLogger(typeof(Configuration));

        /// <summary>
        ///     The location the file should be saved and read from.
        /// </summary>
        private static readonly string CONFIG_FILENAME = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? string.Empty, "nullinside", "ttv-analytics", "config.json");

        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static Configuration? instance;

        private DatabaseConfiguration? databaseConfig;

        /// <summary>
        ///     The OAuth token information.
        /// </summary>
        private OAuthToken? oauth;

        /// <summary>
        ///     The twitchUsername that we are tracking.
        /// </summary>
        private string? twitchUsername;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Configuration" /> class.
        /// </summary>
        protected Configuration() { }

        /// <summary>
        ///     Gets the singleton instance of the configuration.
        /// </summary>
        public static Configuration Instance {
            get {
                if (null == Configuration.instance) {
                    Configuration.instance = Configuration.ReadConfiguration();
                    Configuration.instance.PropertyChanged += Configuration.OnPropertyChanged;
                }

                return Configuration.instance;
            }
        }

        /// <summary>
        ///     Gets or sets the twitchUsername that we are tracking.
        /// </summary>
        public string? TwitchUsername {
            get => this.twitchUsername;
            set {
                this.twitchUsername = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the OAuth token information.
        /// </summary>
        public OAuthToken? OAuth {
            get => this.oauth;
            set {
                this.oauth = value;
                this.OnPropertyChanged();
            }
        }

        public static bool IsTesting { get; set; }

        public DatabaseConfiguration? DatabaseConfig {
            get => this.databaseConfig;
            set {
                this.databaseConfig = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     The event invoked when properties change.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///     Handles cloning this instance of the configuration into a new object.
        /// </summary>
        /// <returns>A new instance of the class.</returns>
        public Configuration Clone() {
            Configuration config = new Configuration { TwitchUsername = this.TwitchUsername, OAuth = this.OAuth?.Clone(), DatabaseConfig = this.DatabaseConfig?.Clone() };
            return config;
        }

        /// <summary>
        ///     Read the configuration from disk.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public static Configuration ReadConfiguration(string? file = null) {
            if (Configuration.IsTesting && null == file) {
                return new Configuration();
            }

            if (null == file) {
                file = Configuration.CONFIG_FILENAME;
            }

            Configuration? config = null;
            try {
                if (File.Exists(file)) {
                    JsonSerializer serializer = new();
                    using (StreamReader sr = new StreamReader(file))
                    using (JsonReader jr = new JsonTextReader(sr)) {
                        config = serializer.Deserialize<Configuration>(jr);

                        // When we read the configuration, the passwords will be in base64 format. Convert them to the real values.
                        if (null != config?.OAuth) {
                            config.OAuth = new OAuthToken(
                                Encoding.UTF8.GetString(Convert.FromBase64String(config.OAuth.Token)),
                                Encoding.UTF8.GetString(Convert.FromBase64String(config.OAuth.RefreshToken)),
                                config.OAuth.TokenExpiration
                            );
                        }

                        if (null != config?.DatabaseConfig) {
                            config.DatabaseConfig = new DatabaseConfiguration(
                                config.DatabaseConfig.Server,
                                config.DatabaseConfig.Username,
                                Encoding.UTF8.GetString(Convert.FromBase64String(config.DatabaseConfig.Password)),
                                config.DatabaseConfig.Database
                            );
                        }
                    }
                }
            } catch (Exception) { }

            // If the file doesn't exist or is invalid, make a new configuration.
            if (null == config) {
                config = new Configuration();
            }

            if (Configuration.IsTesting) {
                Configuration.instance = config;
            }

            return config;
        }

        /// <summary>
        ///     Write the configuration to disk.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool WriteConfiguration() {
            if (Configuration.IsTesting) {
                return false;
            }

            try {
                var dirName = Path.GetDirectoryName(Configuration.CONFIG_FILENAME);
                if (null != dirName && !Directory.Exists(dirName)) {
                    Directory.CreateDirectory(dirName);
                }

                JsonSerializer serializer = new();
                using (StreamWriter sr = new StreamWriter(Configuration.CONFIG_FILENAME))
                using (JsonWriter jr = new JsonTextWriter(sr)) {
                    // When we write the configuration, the passwords will be in plain text format. Convert them to base64 before we write them.
                    // To do this, we cannot update the values in the singleton object. We need to clone the object, make the changes, and then write the clone.
                    var clone = this.Clone();
                    if (null != clone.OAuth) {
                        clone.OAuth = new OAuthToken(
                            Convert.ToBase64String(Encoding.UTF8.GetBytes(clone.OAuth.Token)),
                            Convert.ToBase64String(Encoding.UTF8.GetBytes(clone.OAuth.RefreshToken)),
                            clone.OAuth.TokenExpiration
                        );
                    }

                    if (null != clone.DatabaseConfig) {
                        clone.DatabaseConfig = new DatabaseConfiguration(
                            clone.DatabaseConfig.Server,
                            clone.DatabaseConfig.Username,
                            null != clone.DatabaseConfig.Password ? Convert.ToBase64String(Encoding.UTF8.GetBytes(clone.DatabaseConfig.Password)) : "",
                            clone.DatabaseConfig.Database
                        );
                    }

                    serializer.Serialize(jr, clone);
                }
            } catch (Exception e) {
                Configuration.LOG.Error("Failed to write configuration", e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Handles updating subscribers when properties change.
        /// </summary>
        /// <param name="propertyName">The property name, if not specified it will magically be figured out.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        ///     Handles writing to the configuration file when the properties change.
        /// </summary>
        /// <param name="sender">The singleton instance of this class.</param>
        /// <param name="e">The property arguments.</param>
        private static void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            Configuration.instance?.WriteConfiguration();
        }

        /// <summary>
        ///     A representation of an OAuth token.
        /// </summary>
        public class OAuthToken {
            /// <summary>
            ///     Initializes a new instance of the <see cref="OAuthToken" /> class.
            /// </summary>
            /// <param name="token">The OAuth token.</param>
            /// <param name="refreshToken">The token used to refresh the OAuth token when it expires.</param>
            /// <param name="expiration">The date and time when the OAuth token has expired.</param>
            public OAuthToken(string token, string refreshToken, DateTime expiration) {
                this.Token = token;
                this.RefreshToken = refreshToken;
                this.TokenExpiration = expiration;
            }

            /// <summary>
            ///     Gets the OAuth token.
            /// </summary>
            public string Token { get; }

            /// <summary>
            ///     Gets the token used to refresh the OAuth token when it expires.
            /// </summary>
            public string RefreshToken { get; }

            /// <summary>
            ///     Gets the date and time when the OAuth token has expired.
            /// </summary>
            public DateTime TokenExpiration { get; }

            /// <summary>
            ///     Clones this object.
            /// </summary>
            /// <returns>The new object.</returns>
            public OAuthToken Clone() {
                return new OAuthToken(this.Token, this.RefreshToken, this.TokenExpiration);
            }
        }

        public class DatabaseConfiguration {
            public DatabaseConfiguration(string server, string username, string password, string database) {
                this.Server = server;
                this.Username = username;
                this.Password = password;
                this.Database = database;
            }

            public string Server { get; }
            public string Username { get; }
            public string Password { get; }
            public string Database { get; }

            /// <summary>
            ///     Clones this object.
            /// </summary>
            /// <returns>The new object.</returns>
            public DatabaseConfiguration Clone() {
                return new DatabaseConfiguration(this.Server, this.Username, this.Password, this.Database);
            }
        }
    }
}