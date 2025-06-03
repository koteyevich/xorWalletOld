using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using xorWallet.Commands;
using xorWallet.Processors;
using xorWallet.Utils;

namespace xorWallet
{
    public static class Program
    {
        private static TelegramBotClient? _bot;
        private static CancellationTokenSource? _cts;
        private static long _botId;
        private static CommandRegistry? _commandRegistry;

        public static async Task Main()
        {
            Logger.Bot("Bot starting", "INFO");

            _cts = new CancellationTokenSource();
            _bot = new TelegramBotClient(Secrets.Token);
            var me = await _bot.GetMe();
            _botId = me.Id;

            Logger.Bot($"Bot connected as @{me.Username}", "SUCCESS");

            _commandRegistry = new CommandRegistry();

            _bot.OnMessage += async (message, _) => { await OnMessage(message); };
            _bot.OnUpdate += OnUpdate;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => _cts?.Cancel();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cts?.Cancel();
            };

            await Task.Delay(Timeout.Infinite, _cts.Token);
            Logger.Bot("Bot shutting down", "INFO");
        }

        private static async Task OnMessage(Message message)
        {
            try
            {
                if (message.Text == null)
                {
                    return;
                }

                if (message.Text.StartsWith("/"))
                {
                    if (message.Text.StartsWith("/start"))
                    {
                        if (_bot != null) await StartProcessor.ProcessStartAsync(message, _bot);
                    }
                    else
                    {
                        if (_bot != null) await _commandRegistry?.HandleCommandAsync(message, _bot)!;
                    }
                }
            }
            catch (Exception ex)
            {
                await OnError(ex, message.Chat.Id);
            }
        }

        private static async Task OnUpdate(Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                var decrypted = Encryption.DecryptCallback(update.CallbackQuery?.Data!);
                Logger.Log($"Raw callback data: {update.CallbackQuery}", "CALLBACK", "DEBUG");
                Logger.Log($"Decrypted callback data: {(decrypted ?? "null")}", "CALLBACK", "DEBUG");

                //if (decrypted != null && decrypted.StartsWith("captcha_"))
                //{
                //    await CaptchaProcessor.HandleCaptchaCallback(update.CallbackQuery, _bot, Db);
                //}
            }
        }


        // this here is where your creativity can shine.
        // i chose to send the message that something wrong happened, and send a detailed report in my chat.
        private static async Task OnError(Exception exception, long chatId)
        {
            if (_bot != null)
            {
                await _bot.SendMessage(chatId,
                    $"<b>Ах!</b> <i>Что-то пошло не так...</i> Проблема была автоматически передана разработчикам.\n<blockquote expandable><i>{exception.Message}</i></blockquote>",
                    ParseMode.Html);

                var errorReport = new StringBuilder();
                errorReport.AppendLine("<b>🚨 Ой!</b>");
                errorReport.AppendLine($"<b>Timestamp:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

                errorReport.AppendLine("\n<b>💥 Детали ошибки:</b>");
                errorReport.AppendLine($"Тип: {exception.GetType().FullName}");
                errorReport.AppendLine($"Сообщение: {exception.Message}");

                errorReport.AppendLine("\n<b>📚 Stack Trace:</b>");
                errorReport.AppendLine($"<pre>{exception.StackTrace}</pre>");

                if (exception.InnerException != null)
                {
                    errorReport.AppendLine("\n<b>🔍 Внутренняя ошибка:</b>");
                    errorReport.AppendLine($"Тип: {exception.InnerException.GetType().FullName}");
                    errorReport.AppendLine($"Сообщение: {exception.InnerException.Message}");
                    errorReport.AppendLine($"<pre>{exception.InnerException.StackTrace}</pre>");
                }

                errorReport.AppendLine("\n<b>💻 Информация про систему на которой бот:</b>");
                errorReport.AppendLine($"OS: {Environment.OSVersion}");
                errorReport.AppendLine($"Имя Машины: {Environment.MachineName}");
                errorReport.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                errorReport.AppendLine($"Количество Ядер: {Environment.ProcessorCount}");
                errorReport.AppendLine($"Версия .NET: {Environment.Version}");
                errorReport.AppendLine($"ОЗУ занято ботом: {Environment.WorkingSet / 1024 / 1024}MB");

                try
                {
                    const int maxLength = 4096;
                    var reportParts = SplitMessage(errorReport.ToString(), maxLength);

                    foreach (var part in reportParts)
                    {
                        await _bot.SendMessage(
                            chatId: -1002589303034,
                            text: part,
                            parseMode: ParseMode.Html,
                            messageThreadId: 80
                        );
                        await Task.Delay(500);
                    }
                }
                catch (Exception reportEx)
                {
                    await _bot.SendMessage(
                        chatId: -1002589303034,
                        text: $"Failed to send formatted error report: {reportEx.Message}\n\nRaw error: {exception}",
                        messageThreadId: 80
                    );
                }
            }

            if (_cts != null) await Task.Delay(2000, _cts.Token);
        }

        private static IEnumerable<string> SplitMessage(string message, int maxLength)
        {
            for (var i = 0; i < message.Length; i += maxLength)
            {
                yield return message.Substring(i, Math.Min(maxLength, message.Length - i));
            }
        }
    }
}