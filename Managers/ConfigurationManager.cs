using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

namespace EmojiBot.Managers
{
    public class ConfigurationManager
    {
        public void Init()
        {
            var builder = new ConfigurationBuilder() // Begin building the configuration file
                .SetBasePath(AppContext.BaseDirectory) // Specify the location of the config
                .AddJsonFile("_configuration.json"); // Add the configuration file
            IConfigurationRoot config = builder.Build(); // Build the configuration file

            // Get the discord token from the config file
            string discordToken = config["discord:token"];
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new ArgumentException("Please enter your bot's token into the `_configuration.json` file found in the applications root directory.");
            DiscordToken = discordToken;
            
            // Get the discord channelId IN from the config file
            string channelIdIn = config["discord:channelIdIn"];
            if (string.IsNullOrWhiteSpace(channelIdIn))
                throw new ArgumentException("Please enter your channel's id into the `_configuration.json` file found in the applications root directory.");
            DiscordChannelIdIn = Convert.ToUInt64(channelIdIn);

            // Get the discord channelId Out from the config file
            string channelIdOut = config["discord:channelIdOut"];
            if (string.IsNullOrWhiteSpace(channelIdOut))
                throw new ArgumentException("Please enter your channel's id into the `_configuration.json` file found in the applications root directory.");
            DiscordChannelIdOut = Convert.ToUInt64(channelIdOut);

            // Get the steem curatorId from the config file
            string curatorId = config["discord:curatorId"];
            if (string.IsNullOrWhiteSpace(curatorId))
                throw new ArgumentException("Please enter your curator's id into the `_configuration.json` file found in the applications root directory.");
            SteemCuratorId = Convert.ToUInt64(curatorId);

            // Get the apiyoutubekey from the config file
            string youTubeApiKey = config["youTube:apiKey"];
            if (string.IsNullOrWhiteSpace(youTubeApiKey))
                throw new ArgumentException("Please enter your apikey from youtube into the `_configuration.json` file found in the applications root directory.");
            YouTubeApiKey = youTubeApiKey;
        }

        public string DiscordToken { get; private set; }

        public ulong DiscordChannelIdIn { get; private set; }

        public ulong DiscordChannelIdOut { get; private set; }

        public ulong SteemCuratorId { get; private set; }

        public string YouTubeApiKey { get; private set; }
    }
}