using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

using Ditch;
using Ditch.Helpers;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EmojiBot.Managers;

namespace EmojiBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        private readonly string _channelId;

        private readonly string _authorId;

        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            // Get the discord token from the config file
            string channelId = _config["discord:channelId"];
            if (string.IsNullOrWhiteSpace(channelId))
                throw new Exception("Please enter your channel's id into the `_configuration.json` file found in the applications root directory.");
            _channelId = channelId;

            // Get the discord token from the config file
            string authorId = _config["discord:authorId"];
            if (string.IsNullOrWhiteSpace(authorId))
                throw new Exception("Please enter your author's id into the `_configuration.json` file found in the applications root directory.");
            _authorId = authorId;

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            // si est l'auteuret le channel désiré
            if (msg.Author.Id.ToString() == _authorId && msg.Channel.Id.ToString() == _channelId)
            {
                // ici on est avec un message du dtube bot dans le channel links
                VideoAnalyser.AnalyzeDiscordContent(msg.Content);
            }

            await Task.CompletedTask;
        }
    }
}