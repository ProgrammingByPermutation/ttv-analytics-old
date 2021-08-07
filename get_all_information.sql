SELECT users.username, channels.username, games.`name`, twitch_chat_user_log.joined, twitch_chat_user_log.`left`                                                                                                    FROM twitch_chat_user_log
JOIN twitch_users as users ON users.id = twitch_chat_user_log.twitch_user_id
JOIN twitch_users as channels ON channels.id = twitch_chat_user_log.twitch_channel_id
JOIN twitch_game as games ON games.id = twitch_chat_user_log.twitch_game_id