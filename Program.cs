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

            // Create the service provider
            IServiceProvider provider = services.BuildServiceProvider();

            // Start Discord, steem ...
            await provider.GetRequiredService<StartupService>().StartAsync();

            // Prevent the application from closing
            await Task.Delay(-1);
        }
    }
}