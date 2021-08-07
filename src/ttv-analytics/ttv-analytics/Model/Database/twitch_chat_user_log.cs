namespace TtvAnalytics.Model.Database {
    using System;

    public class twitch_chat_user_log {
        public int id { get; set; }
        public int twitch_channel_id { get; set; }
        public int twitch_user_id { get; set; }
        public int twitch_game_id { get; set; }
        public DateTime joined { get; set; }
        public DateTime? left { get; set; }
    }
}