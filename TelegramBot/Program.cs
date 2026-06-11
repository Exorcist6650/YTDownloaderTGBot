using System;
using AngleSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot;
using YoutubeConnect;

namespace MyApp
{
    internal class Program
    {
        static string GetBotToken(string localVariableName)
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();
            
            var config = builder.Build();
            return config[localVariableName] ?? throw new NullReferenceException($"Not find a {localVariableName} key");
        }

        static void Main(string[] args)
        {
            Host host = new Host(GetBotToken("BOT_TOKEN"), new ConsoleLogger());

            // Bot instance
            YTDownloaderBot YTBot = new YTDownloaderBot(host, 
                new YoutubeReciever(), 
                new ConsoleLogger(), 
                new TelegramLogger());

            YTBot.Init(); // Bot recieving start
            var dm = new DownloadManager();
            dm.DownloadFileAsync("https://vt.tiktok.com/ZSQyaRVXS", DownloadType.Audio);

            Console.ReadLine();
        }
        
    }
}