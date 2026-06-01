using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeConnect;

namespace TelegramBot
{
    public class YTDownloaderBot
    {
        // Dependencies
        private readonly Host _host;
        private readonly YoutubeReciever _ytReciever;
        private readonly ConsoleLogger _consoleLogger;
        private readonly TelegramLogger _telegramLogger;

        // Fields
        private readonly InlineKeyboardMarkup _inlineKeyboard;

        public YTDownloaderBot(Host host, YoutubeReciever ytReciever, ConsoleLogger consoleLogger, TelegramLogger telegramLogger)
        {
            // Binding
            _host = host;
            _ytReciever = ytReciever;
            _consoleLogger = consoleLogger;
            _telegramLogger = telegramLogger;

            // Events
            _host.OnMessage += OnUserSendMessage;
            _host.OnCallback += OnUserClickButton;

            // Fields init
            _inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Download video", "action:video"), 
                    InlineKeyboardButton.WithCallbackData("Download audio", "action:audio"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Cancel", "action:cancel"),

                },
            });
        }


        public void Init()
        {
            _host.Start();
        }

        // Delegates
        private async void OnUserSendMessage(ITelegramBotClient client, Update update)
        {
            string? userMessage = update?.Message?.Text;
            if (userMessage == "/start")
            {
                await _telegramLogger.Log("Send youtube video link", client, update.Message.Chat.Id);
                return;
            }

            // Delete user message
            await client.DeleteMessage(update.Message.Chat.Id, update.Message.Id);

            // Loading and sending info message with buttons
            await LoadAndSendPreviewInfoAsync(client, update.Message);
        }

        private async void OnUserClickButton(ITelegramBotClient client, CallbackQuery cb)
        {
            switch(cb.Data)
            {
                case "action:video":
                    // Loading message for user
                    var LoadingVideoMessage = await _telegramLogger.Log("Video has started download...", client, cb.Message.Chat.Id);

                    // Loading and sending video
                    await LoadAndSendMuxedVideoAsync(client, cb.Message);

                    // Deleteng message for user
                    await client.DeleteMessage(cb.Message.Chat.Id, LoadingVideoMessage.Id);
                    break;


                case "action:audio":
                    // Loading message for user
                    var LoadingAudioMessage = await _telegramLogger.Log("Video has started download...", client, cb.Message.Chat.Id);

                    // Loading and sending audio
                    await LoadAndSendingAudioAsync(client, cb.Message);

                    // Deleteng message for user
                    await client.DeleteMessage(cb.Message.Chat.Id, LoadingAudioMessage.Id);
                    break;


                case "action:cancel":
                    await client.DeleteMessage(cb.Message.Chat.Id, cb.Message.Id);
                    break;
            }


        }

        // Functions
        private async Task<bool> LoadAndSendPreviewInfoAsync(ITelegramBotClient client, Message message)
        {
            // Get async video
            VideoInfo? videoInfo = await _ytReciever.GetVideoInfoAsync(message.Text);
            if (videoInfo != null)
            {
                // Preview image stream
                using var memoryStream = await _ytReciever.GetVideoPreviewStreamAsync(message.Text);
                if (memoryStream != null)
                {
                    // Text сaption
                    string textCaption =
                        $"{videoInfo?.Title}\n" +
                        $"Author: {videoInfo?.Channel}\n" +
                        $"Video duration: {videoInfo?.Duration}\n\n" +
                        videoInfo?.Description +
                        "...";

                    // Sending to chat
                    try
                    {
                        // Telegram message with buttons
                        await client.SendPhoto(message.Chat.Id, InputFile.FromStream(memoryStream), textCaption, replyMarkup: _inlineKeyboard);
                        _consoleLogger.Log("Preview sending sucсessfully");
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
                    {
                        _consoleLogger.Log($"Exceprtion: {ex.Message}", LogStatus.Error);
                    }
                    catch (Exception ex)
                    {
                        _consoleLogger.Log($"Exceprtion: {ex.Message}", LogStatus.Error);
                    }

                    return true;
                }
                else
                {
                    await _telegramLogger.Log("Preview download failed", client, message.Chat.Id, LogStatus.Error);
                    _consoleLogger.Log("Preview stream failde", LogStatus.Error);
                    return false;
                }
            }
            else
            {
                await _telegramLogger.Log("Cannot download this. Please, send a link to youtube video", client, message.Chat.Id);
                return false;
            }
        }

        private async Task<bool> LoadAndSendMuxedVideoAsync(ITelegramBotClient client, Message message)
        {
            // Load video
            using var fileStream = await _ytReciever.GetVideoMuxedStreamAsync(message.Text);
            if (fileStream != null)
            {
                var inputFile = InputFile.FromStream(fileStream, "Video");
                if (inputFile != null)
                {
                    try
                    {
                        // Sending video to chat
                        await client.SendVideo(message.Chat.Id, inputFile, $"@{_host.Me.Username}");
                        _consoleLogger.Log("Video send sucсessfully");
                        return true;
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
                    {
                        _consoleLogger.Log($"Exceprtion: {ex.Message}", LogStatus.Error);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        _telegramLogger?.Log(ex.Message, client, message.Chat.Id, LogStatus.Error);
                        _consoleLogger.Log($"Exceprtion: {ex.Message}", LogStatus.Error);
                        return false;
                    }
                }
                else
                {
                    _consoleLogger.Log($"Video convertation from stream failed", LogStatus.Error);
                    return false;
                }
            }
            else
            {
                _consoleLogger.Log($"Video stream is null", LogStatus.Error);
                return false;
            }
        }
        private async Task<bool> LoadAndSendingAudioAsync(ITelegramBotClient client, Message message)
        {
            // Load video
            using var fileStream = await _ytReciever.GetAudioStreamAsync(message.Text);
            if (fileStream != null)
            {
                var inputFile = InputFile.FromStream(fileStream, "Audio");
                if (inputFile != null)
                {
                    try
                    {
                        // Sending audio to chat
                        await client.SendAudio(message.Chat.Id, inputFile, $"@{_host.Me.Username}");
                        _consoleLogger.Log("Audio send sucсessfully");
                        return true;
                    }
                    catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
                    {
                        _consoleLogger.Log($"Exceprtion: {ex.Message}", LogStatus.Error);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        _telegramLogger?.Log(ex.Message, client, message.Chat.Id, LogStatus.Error);
                        _consoleLogger.Log($"Exceprtion: {ex.Message}", LogStatus.Error);
                        return false;
                    }
                }
                else
                {
                    _consoleLogger.Log($"Audio convertation from stream failed", LogStatus.Error);
                    return false;
                }
            }
            else
            {
                _consoleLogger.Log($"Audio stream is null", LogStatus.Error);
                return false;
            }
        }
    }
}
