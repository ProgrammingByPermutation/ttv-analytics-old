namespace ttv_analytics_test {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using MySql.Data.MySqlClient;
    using NUnit.Framework;
    using TtvAnalytics;
    using TtvAnalytics.Model;
    using TtvAnalytics.Model.Database;
    using TtvAnalytics.Utilities;

    public class DatabaseManagerTests {
        [SetUp]
        public void Setup() {
            Configuration.IsTesting = true;
            Configuration.ReadConfiguration(Path.Join(TestContext.CurrentContext.TestDirectory, "config.json"));
            this.ClearDatabase();
        }

        [TearDown]
        public void TearDown() {
            this.ClearDatabase();
        }

        private void ClearDatabase() {
            using (var conn = new MySqlConnection(DatabaseManagerTests.GetConnectionString())) {
                conn.Open();
                foreach (var sql in new[] {
                    "DELETE FROM twitch_users WHERE 1=1;",
                    "DELETE FROM twitch_game WHERE 1=1;",
                    "DELETE FROM twitch_chat_user_log WHERE 1=1;"
                }) {
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

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

        [Test]
        public async Task InsertingUsers() {
            // Create new users
            var newUsers = new[] { "hi", "how", "are", "you" };
            var usersInDb = await DatabaseManager.GetOrCreateTwitchUsers(newUsers);
            Assert.IsNotNull(usersInDb, "Failed to query database");
            CollectionAssert.AreEquivalent(newUsers, usersInDb.Select(u => u.username), "Did not insert new users correctly");

            // Ensure we don't get duplicates.
            var addUsers = new[] { "hi", "i", "are", "good" };
            var addedUsersInDb = await DatabaseManager.GetOrCreateTwitchUsers(addUsers);
            Assert.IsNotNull(addedUsersInDb, "Failed to query database");
            CollectionAssert.AreEquivalent(addUsers, addedUsersInDb.Select(u => u.username), "Did add additional users correctly");

            foreach (var user in new[] { "hi", "are" }) {
                Assert.AreEqual(
                    usersInDb.FirstOrDefault(u => u.username.Equals(user, StringComparison.InvariantCultureIgnoreCase))?.id ?? -1,
                    addedUsersInDb.FirstOrDefault(u => u.username.Equals(user, StringComparison.InvariantCultureIgnoreCase))?.id ?? -2,
                    "User had different primary keys between database calls"
                );
            }
        }

        [Test]
        public async Task InsertingGames() {
            // Create new games
            var newGames = new[] { "hi", "how", "are", "you" };
            var gamesInDb = await DatabaseManager.GetOrCreateTwitchGames(newGames);
            Assert.IsNotNull(gamesInDb, "Failed to query database");
            CollectionAssert.AreEquivalent(newGames, gamesInDb.Select(u => u.name), "Did not insert new games correctly");

            // Ensure we don't get duplicates.
            var addGames = new[] { "hi", "i", "are", "good" };
            var addedGamesInDb = await DatabaseManager.GetOrCreateTwitchGames(addGames);
            Assert.IsNotNull(addedGamesInDb, "Failed to query database");
            CollectionAssert.AreEquivalent(addGames, addedGamesInDb.Select(u => u.name), "Did add additional games correctly");

            foreach (var game in new[] { "hi", "are" }) {
                Assert.AreEqual(
                    gamesInDb.FirstOrDefault(u => u.name.Equals(game, StringComparison.InvariantCultureIgnoreCase))?.id ?? -1,
                    addedGamesInDb.FirstOrDefault(u => u.name.Equals(game, StringComparison.InvariantCultureIgnoreCase))?.id ?? -2,
                    "Game had different primary keys between database calls"
                );
            }
        }

        [Test]
        public async Task InsertingLogs() {
            /////////////////////////////////////////////////////////////////////
            // Create a situation where tek and bear are watching ox's stream. //
            /////////////////////////////////////////////////////////////////////
            var game = "Path of Exile";
            var users = new[] { "tek", "bear", "oxcanteven" };
            var channel = "oxcanteven";
            var usersInDb = (await DatabaseManager.GetOrCreateTwitchUsers(users)).ToDictionary(k => k.username);
            var gamesInDb = (await DatabaseManager.GetOrCreateTwitchGames(new[] { game })).ToArray();

            // Add OLD entries for tek and bear being in ox's stream.
            var oldChatLogs = new List<twitch_chat_user_log>();
            for (var i = 1; i < 7; i++) {
                foreach (var user in users) {
                    oldChatLogs.Add(new twitch_chat_user_log {
                        twitch_channel_id = usersInDb[channel].id,
                        twitch_game_id = gamesInDb[0].id,
                        twitch_user_id = usersInDb[user].id,
                        joined = DateTime.Now - new TimeSpan(24 * i, 0, 0),
                        left = DateTime.Now - new TimeSpan(24 * i, 0, 0) + new TimeSpan(1, 0, 0)
                    });
                }
            }

            foreach (var user in users) {
                oldChatLogs.Add(new twitch_chat_user_log {
                    twitch_channel_id = usersInDb[channel].id,
                    twitch_game_id = gamesInDb[0].id,
                    twitch_user_id = usersInDb[user].id,
                    joined = DateTime.Now - new TimeSpan(1, 0, 0),
                    left = DateTime.Now - new TimeSpan(0, 20, 0)
                });
            }

            // Insert and make sure it worked
            Assert.IsTrue(await DatabaseManager.AddChatLogs(oldChatLogs), "Failed to load old entries");

            var allLogs = (await DatabaseManager.GetAllChatLogs())?.ToArray();
            Assert.IsNotNull(allLogs, "Database execution failed");
            Assert.AreEqual(21, allLogs.Length, "Not all entries were inserted into database");

            //////////////////////////////
            // Check for de-duplication //
            //////////////////////////////
            var newChatLogs = new List<TwitchChatUserLog>();

            foreach (var user in users) {
                newChatLogs.Add(new TwitchChatUserLog {
                    Channel = channel,
                    Game = game,
                    Username = user
                });
            }

            // Insert and make sure it worked
            Assert.IsTrue(await DatabaseManager.AddChatLogs(newChatLogs), "Failed to add new entries");

            // Check that we have the same number of entries, the existing entry should have updated. We should not have created a brand new entry.
            allLogs = (await DatabaseManager.GetAllChatLogs())?.ToArray();
            Assert.AreEqual(21, allLogs.Length, "Entries were added instead of being updated.");

            // Check to ensure the update actually took place.
            foreach (var user in users) {
                var entry = await DatabaseManager.GetMostRecentUserLog(user, channel, game);
                Assert.IsNotNull(entry, "Did not find most recent entry for a user");
                Assert.IsTrue(DateTime.Now - entry.left < new TimeSpan(0, 20, 0), "Existing database row did not update with extended time");
            }
        }
    }
}