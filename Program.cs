using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EmojiBot.Managers;
using EmojiBot.Services;

namespace EmojiBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder() // Begin building the configuration file
                .SetBasePath(AppContext.BaseDirectory) // Specify the location of the config
                .AddJsonFile("_configuration.json"); // Add the configuration file
            var config = builder.Build(); // Build the configuration file
            ConfigurationManager.Init(config);

            //VideoAnalyser.AnalyzeSteemContent("fran41691", "5mb8pa5g");
            VideoAnalyser.AnalyzeSteemContent("dragoreznov", "dxculcy2"); //doublon youtube
            //new Program().StartAsync().GetAwaiter().GetResult();
        }

        public async Task StartAsync()
        {
            var services = new ServiceCollection() // Begin building the service provider
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig // Add the discord client to the service provider
                    {
                        LogLevel = LogSeverity.Verbose,
                        MessageCacheSize = 1000 // Tell Discord.Net to cache 1000 messages per channel
                    }))
                .AddSingleton(new CommandService(new CommandServiceConfig // Add the command service to the service provider
                    {
                        DefaultRunMode = RunMode.Async, // Force all commands to run async
                        LogLevel = LogSeverity.Verbose
                    }))
                .AddSingleton<CommandHandler>() // Add remaining services to the provider
                .AddSingleton<LoggingService>()
                .AddSingleton<StartupService>()
                .AddSingleton<Random>(); // You get better random with a single instance than by creating a new one every time you need it

            var provider = services.BuildServiceProvider(); // Create the service provider

            provider.GetRequiredService<LoggingService>(); // Initialize the logging service, startup service, and command handler
            await provider.GetRequiredService<StartupService>().StartAsync();
            provider.GetRequiredService<CommandHandler>();

            await Task.Delay(-1); // Prevent the application from closing
        }
    }
}