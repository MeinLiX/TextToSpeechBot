using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.InlineQueryResults;

namespace TextToSpeechBot;

internal class TelegramBot
{
    private readonly string TELEGRAM_BOT_TOKEN;
    private readonly GoogleCloud _googleCloud;
    private readonly CancellationToken _ctx;

    private TelegramBotClient? Bot;

    public TelegramBot(string? TELEGRAM_BOT_TOKEN, GoogleCloud googleCloud, CancellationToken ctx)
    {
        this.TELEGRAM_BOT_TOKEN = TELEGRAM_BOT_TOKEN ?? throw new ArgumentNullException();
        _googleCloud = googleCloud;
        _ctx = ctx;
    }

    public async Task StartAsync()
    {
        Bot = new TelegramBotClient(TELEGRAM_BOT_TOKEN);
        User me = await Bot.GetMeAsync();

        Handler handler = new(_googleCloud);
        Bot.StartReceiving(new DefaultUpdateHandler(handler.HandleUpdateAsync, Handler.HandleErrorAsync),
                           _ctx);

        Console.WriteLine($"Start listening for @{me.Username}");
    }

    internal class Handler
    {
        private readonly GoogleCloud _googleCloud;
        public Handler(GoogleCloud googleCloud)
        {
            _googleCloud = googleCloud;
        }
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage),
                UpdateType.InlineQuery => BotOnInlineQuery(botClient, update.InlineQuery),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private async Task BotOnInlineQuery(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            Console.WriteLine($"Receive inline query type. From: {inlineQuery.From.Username}");

            if (inlineQuery.Query == null)
                return;
            if (inlineQuery.Query == "")
                return;

            Stream SpeechStream = _googleCloud.GetSpeech(inlineQuery.Query);
            string url = await _googleCloud.UploadFileAsync(SpeechStream, $"{inlineQuery.Query}.mp3");
            InlineQueryResultBase[] results = {
                new  InlineQueryResultVoice("1",url,inlineQuery.Query)
            };

            try
            {
                await botClient.AnswerInlineQueryAsync(inlineQuery.Id, results, cacheTime: 55);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error responding to Inline Query! {ex.Message}");
            }
        }

        private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            Console.WriteLine($"Receive message type: {message.Type}. From: {message.From.Username}");

            if (message.Type != MessageType.Text)
            {
                return;
            }

            string helpMessage = "Write me a message and I will try to voice it to you.";


            if (message.Text.StartsWith("/"))
            {
                await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                     text: helpMessage);
                return;
            }

            Stream SpeechStream = _googleCloud.GetSpeech(message.Text);
            InputFileStream file = new(SpeechStream);

            await botClient.SendVoiceAsync(chatId: message.Chat.Id, file.Content);
        }

        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unsupported update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
