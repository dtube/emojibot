using System;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace EmojiBot.Services
{
    public class DiscordLoggingService
    {
        private readonly DiscordSocketClient _discord;

        private string _logDirectory
        {
            get;
        }
        
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.log");

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public DiscordLoggingService(DiscordSocketClient discord)
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discord = discord;
            _discord.Log += OnLogAsync;
        }

        private async Task OnLogAsync(LogMessage msg)
        {
            // Create the log directory if it doesn't exist
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
            // Create today's log file if it doesn't exist
            if (!File.Exists(_logFile))
                File.Create(_logFile).Dispose();

            string logText = $"{DateTime.UtcNow.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            // Write the log text to a file
            await File.AppendAllTextAsync(_logFile, logText + "\n");

            // Write the log text to the console
            await Console.Out.WriteLineAsync(logText);
        }
    }
}