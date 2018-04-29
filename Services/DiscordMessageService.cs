using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

using Ditch;
using Ditch.Steem.Helpers;

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
        private static ISocketMessageChannel _outputMessageChannel;

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
            SocketChannel socketChannel = _discordClient.GetChannel(_configurationManager.DiscordChannelIdOut);
            _outputMessageChannel = socketChannel as ISocketMessageChannel;
            await Console.Out.WriteLineAsync("DiscordOutputChannel : " + _outputMessageChannel?.Name);
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            // si ça vient d'un autre channel, ne pas prendre en compte
            if(msg.Channel.Id != _configurationManager.DiscordChannelIdIn || msg.Author.IsBot)
                return;

            if(!msg.Content.StartsWith("!"))
                return;

            string message = await _videoAnalyser.AnalyzeFromDiscordUserMessage(msg.Author.Username, msg.Content);
            await SendMessageAsync(message);
        }

        public static async Task SendMessageAsync(string message)
        {
            if(_outputMessageChannel == null || string.IsNullOrWhiteSpace(message))
                return;
            
            await Console.Out.WriteLineAsync("Discord send message : " + message);
            try
            {
                await _outputMessageChannel.SendMessageAsync(message);
            }
            catch
            {
                //todo log
            }
        }
    }
}