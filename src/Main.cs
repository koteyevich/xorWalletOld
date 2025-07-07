using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using xorWallet.BotFunctionality.Callbacks;
using xorWallet.BotFunctionality.Commands;
using xorWallet.BotFunctionality.Inline;
using xorWallet.BotFunctionality.Processors;
using xorWallet.Utils;

namespace xorWallet
{
    public static class Program
    {
        private static TelegramBotClient? bot;
        private static CancellationTokenSource? cts;

        private static CommandRegistry? commandRegistry;
        private static CallbackRegistry? callbackRegistry;
        private static InlineRegistry? inlineRegistry;

        public static async Task Main()
        {
            Logger.Bot("Bot starting", "INFO");

            cts = new CancellationTokenSource();

            switch (Secrets.SERVER)
            {
                case Server.Test:
                    bot = new TelegramBotClient(new TelegramBotClientOptions(Secrets.TEST_TOKEN,
                        useTestEnvironment: true));
                    break;
                case Server.Production:
                    bot = new TelegramBotClient(Secrets.PRODUCTION_TOKEN);
                    break;
            }


            var me = await bot.GetMe();

            Logger.Bot($"Bot connected as @{me.Username}", "SUCCESS");

            commandRegistry = new CommandRegistry();
            callbackRegistry = new CallbackRegistry();
            inlineRegistry = new InlineRegistry();

            bot.OnMessage += async (message, _) => { await OnMessage(message); };
            bot.OnUpdate += OnUpdate;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => cts?.Cancel();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts?.Cancel();
            };

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (Exception)
            {
                Logger.Bot("Bot shutting down", "INFO");
            }
        }

        private static async Task OnMessage(Message message)
        {
            try
            {
                if (message.SuccessfulPayment != null)
                {
                    Dictionary<string, int> stars = new Dictionary<string, int>
                    {
                        { "purchase-15-xor", 10 },
                        { "purchase-30-xor", 20 },
                        { "purchase-50-xor", 35 }
                    };

                    Dictionary<string, int> xors = new Dictionary<string, int>
                    {
                        { "purchase-15-xor", 15 },
                        { "purchase-30-xor", 30 },
                        { "purchase-50-xor", 50 }
                    };

                    string payload = message.SuccessfulPayment.InvoicePayload;

                    if (!stars.ContainsKey(payload) || !xors.TryGetValue(payload, out int xor))
                    {
                        throw new Exception($"Invalid invoice payload: {payload}");
                    }

                    await bot!.SendMessage(message.Chat.Id,
                        $"🙏 Спасибо за оплату {stars[payload]} звёзд! \n" +
                        $"💰 {xor} XOR были зачислены на ваш кошелёк.");

                    var db = new Database();

                    await db.UpdateBalanceAsync(message.From!.Id, xors[payload]);
                }

                if (message.Text == null)
                {
                    return;
                }

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
                        await CheckProcessor.CheckAsync(userMessage, bot!);
                    }

                    if (originalText?.Contains("Введите количество XOR (пример: 10)") == true)
                    {
                        var userMessage = message;
                        userMessage.Text = $"/invoice {userMessage.Text}";
                        await InvoiceProcessor.InvoiceAsync(userMessage, bot!);
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
            Console.WriteLine($"Received update: {update.Type}, InlineQuery: {update.InlineQuery?.Query}");
            switch (update.Type)
            {
                case UpdateType.CallbackQuery:
                    try
                    {
                        await callbackRegistry?.HandleCallbackAsync(update.CallbackQuery!, bot!)!;
                    }
                    catch (Exception ex)
                    {
                        await OnError(ex, update.CallbackQuery!.Message!.Chat.Id);
                    }

                    break;
                case UpdateType.InlineQuery:
                    Console.WriteLine($"Processing inline query: {update.InlineQuery?.Query}");
                    await OnInlineQuery(update.InlineQuery!);
                    break;
                case UpdateType.ChosenInlineResult:
                    await OnChosenInlineResult(update.ChosenInlineResult!);
                    break;
                case UpdateType.PreCheckoutQuery:
                    await bot!.AnswerPreCheckoutQuery(update.PreCheckoutQuery!.Id);
                    break;
            }
        }

        private static async Task OnInlineQuery(InlineQuery query)
        {
            var results = await inlineRegistry?.OnInlineQueryTyped(query, bot);

            await bot.AnswerInlineQuery(query.Id, results, 0, true);
        }

        private static async Task OnChosenInlineResult(ChosenInlineResult chosenResult)
        {
            await inlineRegistry?.OnInlineChosen(bot, chosenResult);
        }

        private static async Task OnError(Exception exception, long chatId)
        {
            if (exception is Exceptions.Message)
            {
                await bot!.SendMessage(chatId,
                    $"<b>Ах!</b> <i>Что-то пошло не так...</i>\n" +
                    $"<blockquote expandable><i>{exception.Message}</i></blockquote>",
                    ParseMode.Html);
                return;
            }

            await bot!.SendMessage(chatId,
                $"<b>Ах!</b> <i>Что-то пошло не так...</i> Проблема была автоматически передана разработчикам.\n" +
                $"<blockquote expandable><i>{exception.Message}</i></blockquote>",
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
