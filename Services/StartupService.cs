using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using EmojiBot.Managers;

using Microsoft.Extensions.Configuration;

namespace EmojiBot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        public StartupService(
            DiscordSocketClient discord,
            CommandService commands)
        {
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {           
            // Login to discord
            await _discord.LoginAsync(TokenType.Bot, ConfigurationManager.DiscordToken);
            
            // Connect to the websocket
            await _discord.StartAsync();
        }
    }
}