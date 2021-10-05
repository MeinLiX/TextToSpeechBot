using TextToSpeechBot;

dotenv.net.DotEnv.Load();

#region required Environments
string? TELEGRAM_BOT_TOKEN = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
string? GOOGLE_APPLICATION_CREDENTIALS = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
string? GOOGLE_APPLICATION_PROJECT_ID = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_PROJECT_ID");
#endregion

using CancellationTokenSource cts = new();

GoogleCloud googleCloud = new(GOOGLE_APPLICATION_CREDENTIALS, GOOGLE_APPLICATION_PROJECT_ID);
TelegramBot telegramBot = new(TELEGRAM_BOT_TOKEN, googleCloud, cts.Token);

await telegramBot.StartAsync();

Console.ReadKey();
cts.Cancel();
