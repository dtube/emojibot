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
        private ISocketMessageChannel _outputMessageChannel;

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

            // si c'est un message venant du bot et du channel désiré
            if (msg.Author.IsBot && msg.Author.Id == _configurationManager.DiscordAuthorId && msg.Channel.Id == _configurationManager.DiscordChannelIdIn)
            {
                await Console.Out.WriteLineAsync("Discord receive message : " + msg.Content);
                List<string> videoMessages = await _videoAnalyser.AnalyzeFromDiscordBotMessage(msg.Content);
                videoMessages.ForEach(async videoMessage => await SendMessageAsync(videoMessage));
            }

            // si c'est un message venant d'un utilisateur et du channel désiré
            else if (!msg.Author.IsBot && msg.Channel.Id == _configurationManager.DiscordChannelIdIn)
            {
                await Console.Out.WriteLineAsync("Discord receive message : " + msg.Content);
                List<string> videoMessages = await _videoAnalyser.AnalyzeFromDiscordUserMessage(msg.Content);
                videoMessages.ForEach(async videoMessage => await SendMessageAsync(videoMessage));
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if(_outputMessageChannel == null || string.IsNullOrWhiteSpace(message))
                return;
            
            await Console.Out.WriteLineAsync("Discord send message : " + message);
            await _outputMessageChannel.SendMessageAsync(message);            
        }
    }
}