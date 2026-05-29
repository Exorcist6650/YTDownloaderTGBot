using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public class Host
    {
        public Action<ITelegramBotClient, Update>? OnMessage; // Event
        private TelegramBotClient _bot; // Bot instance

        // Ctor
        public Host(string token)
        {
            _bot = new TelegramBotClient(token);
        }

        public void Start()
        {
            _bot.StartReceiving(UpdateHandler, ErrorHandler);
            Console.WriteLine("Start receiving");
        }

        // Handlers update methods
        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Console.WriteLine($"User message: {update.Message?.Text}\tUser id: {update.Message.Chat.Id}");
            OnMessage?.Invoke(client, update);
            await Task.CompletedTask;
        }
        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine($"Exception: {exception.Message}");
            await Task.CompletedTask;
        }

        // Public methods
        public async Task SendMessage(ChatId chatId, string message)
        {
            _bot?.SendMessage(chatId, message);
            await Task.CompletedTask;
        }

    }
}
