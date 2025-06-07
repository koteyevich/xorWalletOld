using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using xorWallet.Callbacks;
using xorWallet.Commands;
using xorWallet.Processors;
using xorWallet.Utils;

namespace xorWallet
{
    public static class Program
    {
        private static TelegramBotClient? bot;
        private static CancellationTokenSource? cts;

        private static CommandRegistry? commandRegistry;
        private static CallbackRegistry? callbackRegistry;

        public static async Task Main()
        {
            Logger.Bot("Bot starting", "INFO");

            cts = new CancellationTokenSource();
            bot = new TelegramBotClient(Secrets.TOKEN);
            var me = await bot.GetMe();

            Logger.Bot($"Bot connected as @{me.Username}", "SUCCESS");

            commandRegistry = new CommandRegistry();
            callbackRegistry = new CallbackRegistry();

            bot.OnMessage += async (message, _) => { await OnMessage(message); };
            bot.OnUpdate += OnUpdate;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => cts?.Cancel();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts?.Cancel();
            };

            await Task.Delay(Timeout.Infinite, cts.Token);
            Logger.Bot("Bot shutting down", "INFO");
        }

        private static async Task OnMessage(Message message)
        {
            try
            {
                if (message.Text!.StartsWith("/"))
                {
                    await commandRegistry?.HandleCommandAsync(message, bot!)!;
                }

                if (message.ReplyToMessage != null)
                {
                    string? originalText = message.ReplyToMessage.Text;

                    if (originalText?.Contains("Введите количество XOR и количество активаций") == true)
                    {
                        var userMessage = message;
                        userMessage.Text = $"/check {userMessage.Text}";
                        await CheckProcessor.CheckAsync(userMessage, bot);
                    }

                    if (originalText?.Contains("Введите количество XOR (пример: 10)") == true)
                    {
                        var userMessage = message;
                        userMessage.Text = $"/invoice {userMessage.Text}";
                        await InvoiceProcessor.InvoiceAsync(userMessage, bot);
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
                try
                {
                    await callbackRegistry?.HandleCallbackAsync(update.CallbackQuery!, bot!)!;
                }
                catch (Exception ex)
                {
                    await OnError(ex, update.CallbackQuery!.Message!.Chat.Id);
                }
            }
        }


        // this here is where your creativity can shine.
        // i chose to send the message that something wrong happened, and send a detailed report in my chat.
        private static async Task OnError(Exception exception, long chatId)
        {
            if (exception is Exceptions.Message)
            {
                await bot!.SendMessage(chatId,
                    $"<b>Ах!</b> <i>Что-то пошло не так...</i>\n<blockquote expandable><i>{exception.Message}</i></blockquote>",
                    ParseMode.Html);
                return;
            }

            await bot!.SendMessage(chatId,
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
                const int max_length = 4096;
                var reportParts = splitMessage(errorReport.ToString(), max_length);

                foreach (string part in reportParts)
                {
                    await bot!.SendMessage(
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
                await bot!.SendMessage(
                    chatId: -1002589303034,
                    text:
                    $"Failed to send formatted error report: {reportEx.Message}\n\nRaw error: {exception}",
                    messageThreadId: 80
                );
            }


            if (cts != null) await Task.Delay(2000, cts.Token);
        }

        private static IEnumerable<string> splitMessage(string message, int maxLength)
        {
            for (int i = 0; i < message.Length; i += maxLength)
            {
                yield return message.Substring(i, Math.Min(maxLength, message.Length - i));
            }
        }
    }
}
