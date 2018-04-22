using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Ditch;

using EmojiBot.Managers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmojiBot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly OperationManager _steemClient;
        private readonly ConfigurationManager _configurationManager;
        private readonly IServiceProvider _provider;

        public StartupService(DiscordSocketClient discordClient, 
            OperationManager steemClient, 
            ConfigurationManager configurationManager,
            IServiceProvider provider)
        {
            _discordClient = discordClient;
            _steemClient = steemClient;
            _configurationManager = configurationManager;
            _provider = provider;
        }

        public async Task StartAsync()
        {
            _configurationManager.Init();

            // DISCORD
            _provider.GetRequiredService<DiscordLoggingService>(); // register log events
            _provider.GetRequiredService<DiscordMessageService>(); // register receiver message events            
            await _discordClient.LoginAsync(TokenType.Bot, _configurationManager.DiscordToken); // Login to discord          
            await _discordClient.StartAsync(); // Connect to the websocket          

            // STEEM
            var steem = new List<string>
            {
                //"wss://steemd.steemit.com",       //ko
                "wss://steemd.privex.io",         //ko
                //"wss://steemd.steemitstage.com",  //ok
                //"wss://steemd.steemitdev.com",    //ko
                //"wss://rpc.steemliberator.com",   //ok
                //"wss://steemd.minnowsupportproject.org", //ko
                //"wss://rpc.buildteam.io",         //ok
                //"wss://steemd.pevo.science",      //ok
                //"wss://steemd.steemgigs.org",     //ko
                //"wss://gtg.steem.house",          //ko
                //"wss://rpc.steemviz.com",         //ok
                //"wss://seed.bitcoiner.me",        //ok
                //"wss://steemd-int.steemit.com",     //ok
                //"wss://",
            };

            string urlSteem = _steemClient.TryConnectTo(steem);
            if(string.IsNullOrWhiteSpace(urlSteem))
                await Console.Out.WriteLineAsync($"[SteemConnection] Unable to connecte to Steem's Gateways");
            else
                await Console.Out.WriteLineAsync($"[SteemConnection] Connected to {urlSteem}");
        }
    }
}