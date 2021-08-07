namespace TtvAnalytics.Model.Database {
    using System.Data.Common;
    using System.Data.Entity;
    using MySql.Data.EntityFramework;

    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class TtvAnalyticsContext : DbContext {
        public TtvAnalyticsContext() { }

        public TtvAnalyticsContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection) { }

        public DbSet<twitch_users> Users { get; set; }
        public DbSet<twitch_game> Games { get; set; }
        public DbSet<twitch_chat_user_log> TwitchChatUserLogs { get; set; }
    }
}