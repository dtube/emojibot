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
    public class DiscordMessageService
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly VideoAnalyserService _videoAnalyser;
        private readonly ConfigurationManager _configurationManager;

        public ISocketMessageChannel OutputChannel { get; set; }

        public DiscordMessageService(DiscordSocketClient discordClient, 
            VideoAnalyserService videoAnalyser, 
            ConfigurationManager configurationManager)
        {
            _videoAnalyser = videoAnalyser;
            _discordClient = discordClient;
            _configurationManager = configurationManager;

            _discordClient.Ready += DiscordReady;
            _discordClient.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task DiscordReady()
        {
            SocketChannel t = _discordClient.GetChannel(_configurationManager.DiscordChannelIdOut);
            OutputChannel = t as ISocketMessageChannel;
            await Console.Out.WriteLineAsync("DiscordOutputChannel : " + OutputChannel.Name);
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            // si c'est un message venant de l'auteur et du channel désiré
            if (msg.Author.IsBot && msg.Author.Id == _configurationManager.DiscordAuthorId && msg.Channel.Id == _configurationManager.DiscordChannelIdIn)
            {
                await Console.Out.WriteLineAsync("Discord receive message : " + msg.Content);
                // ici on est avec un message du dtube bot dans le channel links
                string message = await _videoAnalyser.AnalyzeDiscordContent(msg.Content);
                await SendMessageAsync(message);
            }
        }

        private async Task SendMessageAsync(string message)
        {
            // publier sur discord
            if(OutputChannel != null)
            {
                await Console.Out.WriteLineAsync("Discord send message : " + message);
                await OutputChannel.SendMessageAsync(message);
            }
        }
    }
}