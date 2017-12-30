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
        private readonly IServiceProvider _provider;

        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            // si c'est un message venant de l'auteur et du channel désiré
            if (msg.Author.Id == ConfigurationManager.DiscordAuthorId && msg.Channel.Id == ConfigurationManager.DiscordChannelId)
            {
                // ici on est avec un message du dtube bot dans le channel links
                VideoAnalyser.AnalyzeDiscordContent(msg.Content);
            }

            await Task.CompletedTask;
        }
    }
}