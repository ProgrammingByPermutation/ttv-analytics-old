namespace TtvAnalytics.Utilities {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using log4net;
    using Model;
    using Model.Database;
    using MySql.Data.MySqlClient;

    public class DatabaseManager {
        /// <summary>
        ///     The logger.
        /// </summary>
        private static readonly ILog LOG = LogManager.GetLogger(typeof(DatabaseManager));

        private static readonly TimeSpan DISCONNECT_THRESHOLD = new TimeSpan(0, 30, 0);

        private static string? GetConnectionString() {
            var config = Configuration.Instance.DatabaseConfig;
            if (null == config || string.IsNullOrWhiteSpace(config.Server) ||
                string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password) ||
                string.IsNullOrWhiteSpace(config.Database)) {
                return null;
            }

            return new MySqlConnectionStringBuilder {
                Server = config.Server,
                UserID = config.Username,
                Password = config.Password,
                Database = config.Database
            }.ConnectionString;
        }

        private static async Task<T?> PerformDatabaseCall<T>(Func<MySqlConnection, TtvAnalyticsContext?, Task<T?>> databaseMethod, bool createContext) {
            try {
                using (MySqlConnection conn = new MySqlConnection(DatabaseManager.GetConnectionString())) {
                    await conn.OpenAsync();

                    if (createContext) {
                        using (var context = new TtvAnalyticsContext(conn, false)) {
                            return await databaseMethod.Invoke(conn, context);
                        }
                    }

                    return await databaseMethod.Invoke(conn, null);
                }
            } catch (Exception e) {
                DatabaseManager.LOG.Error("Failed to make call to database", e);
            }

            return default;
        }

        public static async Task<IEnumerable<twitch_users>?> GetOrCreateTwitchUsers(IEnumerable<string> usernames) {
            return await DatabaseManager.PerformDatabaseCall<twitch_users[]?>(async (conn, context) => {
                var requestedUsers = usernames.Select(u => u.ToLowerInvariant().Trim()).Distinct();
                var dbExistingUsers = context.Users.Where(u => requestedUsers.Contains(u.username))
                    .Select(u => u.username);
                var newUsers = requestedUsers.Except(dbExistingUsers);

                foreach (var user in newUsers) {
                    context.Users.Add(new twitch_users { username = user });
                }

                context.SaveChanges();
                return context.Users.Where(u => requestedUsers.Contains(u.username)).ToArray();
            }, true);
        }

        public static async Task<IEnumerable<twitch_game>?> GetOrCreateTwitchGames(IEnumerable<string> games) {
            return await DatabaseManager.PerformDatabaseCall<twitch_game[]?>(async (conn, context) => {
                var requestedGames = games.Select(g => g.ToLowerInvariant().Trim()).Distinct();
                var dbExistingGames = context.Games.Where(g => requestedGames.Contains(g.name))
                    .Select(g => g.name);
                var newGames = requestedGames.Except(dbExistingGames);

                foreach (var game in newGames) {
                    context.Games.Add(new twitch_game { name = game });
                }

                context.SaveChanges();
                return context.Games.Where(g => requestedGames.Contains(g.name)).ToArray();
            }, true);
        }

        public static async Task<IEnumerable<twitch_chat_user_log>?> GetAllChatLogs() {
            return await DatabaseManager.PerformDatabaseCall<twitch_chat_user_log[]?>(async (conn, context) => { return context.TwitchChatUserLogs.ToArray(); }, true);
        }

        public static async Task<bool?> AddChatLogs(IEnumerable<TwitchChatUserLog> logs) {
            return await DatabaseManager.PerformDatabaseCall(async (conn, context) => {
                var allLogs = logs.ToArray();
                var justUsernames = allLogs.Select(l => l.Username.ToLowerInvariant()).Distinct();
                var justChannels = allLogs.Select(l => l.Channel.ToLowerInvariant()).Distinct();
                var dbUsers = await DatabaseManager.GetOrCreateTwitchUsers(justUsernames.Concat(justChannels));
                var userDict = dbUsers?.ToDictionary(u => u.username);

                var allGames = allLogs.Select(l => l.Game.ToLowerInvariant()).Distinct();
                var dbGames = await DatabaseManager.GetOrCreateTwitchGames(allGames);
                var gameDict = dbGames?.ToDictionary(u => u.name);

                if (null == userDict || null == gameDict || userDict.Count == 0 || gameDict.Count == 0) {
                    return false;
                }

                var dbRows = new List<twitch_chat_user_log>();
                foreach (var log in logs) {
                    dbRows.Add(new twitch_chat_user_log {
                        twitch_user_id = userDict[log.Username.ToLowerInvariant()].id,
                        twitch_game_id = gameDict[log.Game.ToLowerInvariant()].id,
                        twitch_channel_id = userDict[log.Channel.ToLowerInvariant()].id
                    });
                }

                return await DatabaseManager.AddChatLogs(dbRows);
            }, true);
        }

        public static async Task<bool?> AddChatLogs(IEnumerable<twitch_chat_user_log> logs) {
            return await DatabaseManager.PerformDatabaseCall<bool?>(async (conn, context) => {
                // Get the list of all existing logs.
                var existing = await DatabaseManager.GetLastEntryForUserInChannel(conn, logs);

                using (var ttvContext = new TtvAnalyticsContext(conn, false)) {
                    foreach (var log in logs) {
                        log.joined = DateTime.Now;
                        log.left = DateTime.Now;

                        var mostRecentLog = DatabaseManager.GetUsersLog(existing, log.twitch_user_id, log.twitch_channel_id, log.twitch_game_id);
                        if (null == mostRecentLog || DateTime.Now - mostRecentLog.left > DatabaseManager.DISCONNECT_THRESHOLD) {
                            ttvContext.TwitchChatUserLogs.Add(log);
                            continue;
                        }

                        var existingLog = ttvContext.TwitchChatUserLogs.Find(mostRecentLog.id);
                        if (null == existingLog) {
                            ttvContext.TwitchChatUserLogs.Add(log);
                            continue;
                        }

                        existingLog.left = DateTime.Now;
                    }

                    await ttvContext.SaveChangesAsync();
                }

                return true;
            }, false);
        }

        public static async Task<twitch_chat_user_log?> GetMostRecentUserLog(string username, string channel, string game) {
            return await DatabaseManager.PerformDatabaseCall<twitch_chat_user_log?>(async (conn, context) => {
                twitch_users? userDb;
                twitch_users? channelDb;
                twitch_game? gameDb;
                using (var ttvContext = new TtvAnalyticsContext(conn, false)) {
                    userDb = ttvContext.Users.FirstOrDefault(u => u.username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
                    channelDb = ttvContext.Users.FirstOrDefault(u => u.username.Equals(channel, StringComparison.InvariantCultureIgnoreCase));
                    gameDb = ttvContext.Games.FirstOrDefault(u => u.name.Equals(game, StringComparison.InvariantCultureIgnoreCase));
                }

                if (null == userDb || null == channelDb || null == gameDb) {
                    return null;
                }

                var mockEntry = new[] { new twitch_chat_user_log { twitch_user_id = userDb.id, twitch_channel_id = channelDb.id, twitch_game_id = gameDb.id } };
                return (await DatabaseManager.GetLastEntryForUserInChannel(conn, mockEntry))?.FirstOrDefault();
            }, false);
        }

        private static twitch_chat_user_log? GetUsersLog(IEnumerable<twitch_chat_user_log> allLogs, int user_id, int channel_id, int game_id) {
            return allLogs.FirstOrDefault(l => l.twitch_user_id == user_id && l.twitch_channel_id == channel_id && l.twitch_game_id == game_id);
        }

        private static async Task<IEnumerable<twitch_chat_user_log>?> GetLastEntryForUserInChannel(MySqlConnection conn, IEnumerable<twitch_chat_user_log> logs) {
            const string sql = @"
                        SELECT t1.* from ttv_analytics.twitch_chat_user_log t1
                        JOIN (
	                        SELECT twitch_user_id, twitch_channel_id, twitch_game_id, MAX(`left`) AS min_value
                            from ttv_analytics.twitch_chat_user_log
                            GROUP BY twitch_user_id, twitch_channel_id, twitch_game_id
                        ) as t2 on 
                        t1.twitch_user_id = t2.twitch_user_id AND 
                        t1.twitch_channel_id = t2.twitch_channel_id AND 
                        t1.twitch_game_id = t2.twitch_game_id AND 
                        t1.`left` = t2.min_value;
                ";

            var existingLogs = new List<twitch_chat_user_log>();
            using (var command = conn.CreateCommand()) {
                command.CommandText = sql;
                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        existingLogs.Add(new twitch_chat_user_log {
                            id = int.Parse(reader["id"].ToString() ?? "-1"),
                            joined = (DateTime)reader["joined"],
                            left = (DateTime)reader["left"],
                            twitch_channel_id = int.Parse(reader["twitch_channel_id"].ToString() ?? "-1"),
                            twitch_game_id = int.Parse(reader["twitch_game_id"].ToString() ?? "-1"),
                            twitch_user_id = int.Parse(reader["twitch_user_id"].ToString() ?? "-1")
                        });
                    }
                }
            }

            return existingLogs;
        }
    }
}