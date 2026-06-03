using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public enum LogStatus : byte
    {
        Message, 
        Warning,
        Error,
    }

    public class ConsoleLogger
    {
        public void Log(string message, LogStatus status = LogStatus.Message)
        {
            Console.WriteLine($"{DateTime.UtcNow} | {message} | {status}");
        }
    }

    public class TelegramLogger
    {
        /// <summary>
        /// Log in telegram chat
        /// </summary>
        /// <param name="message"></param>
        /// <param name="client"></param>
        /// <param name="chatID"></param>
        /// <param name="status"></param>
        /// <returns>
        /// Return a bot message reference
        /// </returns>
        public async Task<Message?> Log(string message, ITelegramBotClient client, ChatId chatID, LogStatus status = LogStatus.Message)
        {
            try
            {
                var bot_message = await client.SendMessage(chatID, status == LogStatus.Message ? message
                    : $"{DateTime.UtcNow} | {message} | {status}");
                return bot_message;
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
            {
                Console.WriteLine($"Exception: {ex.Message}", LogStatus.Error);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}", LogStatus.Error);
                return null;
            }

        }
    }
}
