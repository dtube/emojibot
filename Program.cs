using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using EmojiBot.Managers;
using EmojiBot.Services;

using Ditch;

namespace EmojiBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().StartAsync().Wait();

            //var result = VideoAnalyser.AnalyzeDiscordContent(@"te
//https://d.tube/#!/v/shaunonsite/tdybthrb").Result;
            //var result = VideoAnalyser.AnalyzeSteemContent("fran41691", "5mb8pa5g").Result;
            //var result = VideoAnalyser.AnalyzeSteemContent("dragoreznov", "dxculcy2").Result; //doublon youtube
        }

        public async Task StartAsync()
        {
            var services = new ServiceCollection() // Begin building the service provider
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig // Add the discord client to the service provider
                    {
                        LogLevel = LogSeverity.Verbose,
                        MessageCacheSize = 1000 // Tell Discord.Net to cache 1000 messages per channel
                    }))
                .AddSingleton<DiscordMessageService>()
                .AddSingleton<DiscordLoggingService>()
                .AddSingleton<OperationManager>() // Steem client
                .AddSingleton<VideoAnalyserService>()
                .AddSingleton<ConfigurationManager>()
                .AddSingleton<StartupService>();

            IServiceProvider provider = services.BuildServiceProvider(); // Create the service provider

            // Initialize the logging service, startup service, and command handler
            await provider.GetRequiredService<StartupService>().StartAsync();

            await Task.Delay(-1); // Prevent the application from closing
        }
    }
}