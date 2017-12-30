using System;
using Microsoft.Extensions.Configuration;

namespace EmojiBot.Managers
{
    public static class ConfigurationManager
    {
        public static void Init(IConfigurationRoot config)
        {
            // Get the discord token from the config file
            string discordToken = config["discord:token"];
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token into the `_configuration.json` file found in the applications root directory.");
            DiscordToken = discordToken;
            
            // Get the discord token from the config file
            string channelId = config["discord:channelId"];
            if (string.IsNullOrWhiteSpace(channelId))
                throw new Exception("Please enter your channel's id into the `_configuration.json` file found in the applications root directory.");
            DiscordChannelId = Convert.ToUInt64(channelId);

            // Get the discord token from the config file
            string authorId = config["discord:authorId"];
            if (string.IsNullOrWhiteSpace(authorId))
                throw new Exception("Please enter your author's id into the `_configuration.json` file found in the applications root directory.");
            DiscordAuthorId = Convert.ToUInt64(authorId);

            // Get the apiyoutubekey from the config file
            string youTubeApiKey = config["youTube:apiKey"];
            if (string.IsNullOrWhiteSpace(youTubeApiKey))
                throw new Exception("Please enter your apikey from youtube into the `_configuration.json` file found in the applications root directory.");
            YouTubeApiKey = youTubeApiKey;
        }

        public static string DiscordToken { get; private set; }

        public static ulong DiscordChannelId { get; private set; }

        public static ulong DiscordAuthorId { get; private set; }

        public static string YouTubeApiKey { get; private set; }
    }
}