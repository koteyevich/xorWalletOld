using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.BotFunctionality.Callbacks;
using xorWallet.BotFunctionality.Commands;
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
                    await OnInlineQuery(bot!, update.InlineQuery!);
                    break;
                case UpdateType.ChosenInlineResult:
                    await OnChosenInlineResult(bot!, update.ChosenInlineResult!);
                    break;
                case UpdateType.PreCheckoutQuery:
                    await bot!.AnswerPreCheckoutQuery(update.PreCheckoutQuery!.Id);
                    break;
            }
        }

        private static async Task OnInlineQuery(ITelegramBotClient botClient, InlineQuery query)
        {
            Console.WriteLine($"OnInlineQuery called: Query={query.Query}, From={query.From.Username}, Id={query.Id}");
            var results = new List<InlineQueryResultArticle>();

            var db = new Database();
            var user = await db.GetUserAsync(query.From.Id);


            if (string.IsNullOrWhiteSpace(query.Query))
            {
                results.Add(
                    new InlineQueryResultArticle(
                        id: IdGenerator.GenerateId(),
                        title: $"Ваш баланс: {user.Balance} XOR",
                        inputMessageContent: new InputTextMessageContent($"Мой баланс: {user.Balance} XOR")
                        {
                            ParseMode = ParseMode.Markdown
                        }
                    )
                    {
                        Description = "Ваш текущий баланс",
                    }
                );

                results.Add(new InlineQueryResultArticle(
                    id: IdGenerator.GenerateId(),
                    title: $"Помощь", new InputTextMessageContent($"Помощь"))
                {
                    Description = "@xorwallet_bot pay 15 - создать счёт, @xorwallet_bot give 5 3 - создать чек"
                });
            }

            if (query.Query.StartsWith("pay", StringComparison.OrdinalIgnoreCase))
            {
                string cleanedQuery = query.Query["pay".Length..].Trim();

                if (decimal.TryParse(cleanedQuery, out decimal decimalAmount))
                {
                    int flooredAmount = (int)Math.Floor(decimalAmount);
                    string messageText = $"Счёт на {flooredAmount} XOR";
                    string description = "Нажми чтобы создать счёт.";

                    if (decimalAmount != flooredAmount)
                    {
                        description = $"⚠️ {decimalAmount} будет конвертировано в {flooredAmount}";
                    }

                    var inlineKeyboard = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("⏳ Создание Счёта",
                            $"null")
                    );

                    results.Add(
                        new InlineQueryResultArticle(
                            id: IdGenerator.GenerateId(),
                            title: $"Счёт на {flooredAmount} XOR",
                            inputMessageContent: new InputTextMessageContent(messageText)
                            {
                                ParseMode = ParseMode.Markdown
                            }
                        )
                        {
                            Description = description,
                            ReplyMarkup = inlineKeyboard
                        }
                    );
                }
                else
                {
                    results.Add(
                        new InlineQueryResultArticle(
                            id: IdGenerator.GenerateId(),
                            title: "Неверное количество",
                            inputMessageContent: new InputTextMessageContent(
                                "Неправильный формат. Попробуйте: @xorwallet_bot pay 10")
                        )
                        {
                            Description = "Пожалуйста, введите корректное количество после 'pay'"
                        }
                    );
                }
            }

            if (query.Query.StartsWith("give", StringComparison.OrdinalIgnoreCase))
            {
                string[] cleanedQuery = query.Query.Replace("give", string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (decimal.TryParse(cleanedQuery[0], out decimal xorAmount))
                {
                    decimal.TryParse(cleanedQuery[1], out decimal activationAmount);

                    int flooredXorAmount = (int)Math.Floor(xorAmount);
                    int flooredActivationAmount = (int)Math.Floor(activationAmount);
                    string messageText = $"Чек на {flooredXorAmount} XOR ({flooredActivationAmount} акт.)";
                    string description = "Нажми чтобы создать чек.";

                    if (user.Balance < flooredXorAmount * flooredActivationAmount)
                    {
                        description = $"Нужно {flooredXorAmount * flooredActivationAmount} XOR для чека.";
                    }

                    if (xorAmount != flooredXorAmount)
                    {
                        description = $"⚠️ {xorAmount} будет конвертировано в {flooredXorAmount}\n";
                    }

                    if (activationAmount != flooredActivationAmount)
                    {
                        description += $"{activationAmount} будет конвертировано в {flooredActivationAmount}\n";
                    }

                    var inlineKeyboard = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("⏳ Создание Чека",
                            $"null")
                    );

                    if (user.Balance < flooredXorAmount * flooredActivationAmount)
                    {
                        results.Add(
                            new InlineQueryResultArticle(
                                id: IdGenerator.GenerateId(),
                                title: "Недостаточно XOR.",
                                inputMessageContent: new InputTextMessageContent(
                                    $"Недостаточно XOR.\nВы создаёте чек на: {flooredXorAmount * flooredActivationAmount} ({flooredXorAmount} * {flooredActivationAmount})\nВаш баланс: {user.Balance}")
                            )
                            {
                                Description = description,
                            }
                        );
                    }
                    else
                    {
                        results.Add(
                            new InlineQueryResultArticle(
                                id: IdGenerator.GenerateId(),
                                title: $"Чек на {flooredXorAmount} XOR ({activationAmount} активаций)",
                                inputMessageContent: new InputTextMessageContent(messageText)
                                {
                                    ParseMode = ParseMode.Markdown
                                }
                            )
                            {
                                Description = description,
                                ReplyMarkup = inlineKeyboard
                            }
                        );
                    }
                }
                else
                {
                    results.Add(
                        new InlineQueryResultArticle(
                            id: IdGenerator.GenerateId(),
                            title: "Неверное количество",
                            inputMessageContent: new InputTextMessageContent(
                                "Неправильный формат. Попробуйте: @xorwallet_bot give 3 5")
                        )
                        {
                            Description = "Пожалуйста, введите корректное количество после 'give'"
                        }
                    );
                }
            }

            try
            {
                await botClient.AnswerInlineQuery(query.Id, results, isPersonal: true, cacheTime: 0);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                int retryAfter = ex.Parameters?.RetryAfter ?? 1;
                Logger.Warn($"Rate limit hit, retrying after {retryAfter} seconds");
                await Task.Delay(retryAfter * 1000);
                await botClient.AnswerInlineQuery(query.Id, results, isPersonal: true);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error in OnInlineQuery: {ex.Message}\nStackTrace: {ex.StackTrace}");
                // Handle network errors
                if (ex is HttpRequestException or System.Net.Sockets.SocketException)
                {
                    Logger.Warn("Network error detected, retrying in 5 seconds");
                    await Task.Delay(5000);
                    await botClient.AnswerInlineQuery(query.Id, results, isPersonal: true);
                }
            }
        }

        private static async Task OnChosenInlineResult(ITelegramBotClient botClient, ChosenInlineResult chosenResult)
        {
            Console.WriteLine(
                $"User {chosenResult.From.Username} chose result: {chosenResult.ResultId}, Query: {chosenResult.Query}");

            if (chosenResult.Query.StartsWith("pay", StringComparison.OrdinalIgnoreCase))
            {
                string cleanedQuery = chosenResult.Query["pay".Length..].Trim();
                if (decimal.TryParse(cleanedQuery, out decimal decimalAmount))
                {
                    int flooredAmount = (int)Math.Floor(decimalAmount);
                    var db = new Database();

                    string invoiceId = await db.CreateInvoiceAsync(chosenResult.From.Id, flooredAmount);

                    var updatedKeyboard = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithUrl("💰 Оплатить счёт",
                            $"{StartUrlGenerator.GenerateStartUrl(invoiceId)}")
                    );

                    await botClient.EditMessageReplyMarkup(
                        inlineMessageId: chosenResult.InlineMessageId!,
                        replyMarkup: updatedKeyboard
                    );
                }
            }

            if (chosenResult.Query.StartsWith("give", StringComparison.OrdinalIgnoreCase))
            {
                string[] cleanedQuery = chosenResult.Query.Replace("give", string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (decimal.TryParse(cleanedQuery[0], out decimal decimalXor))
                {
                    decimal.TryParse(cleanedQuery[1], out decimal activationAmount);

                    int flooredAmount = (int)Math.Floor(decimalXor);
                    int flooredActivationAmount = (int)Math.Floor(activationAmount);
                    var db = new Database();

                    string checkId =
                        await db.CreateCheckAsync(chosenResult.From.Id, flooredAmount, flooredActivationAmount);

                    var updatedKeyboard = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithUrl("💰 Активировать чек",
                            $"{StartUrlGenerator.GenerateStartUrl(checkId)}")
                    );

                    await botClient.EditMessageReplyMarkup(
                        inlineMessageId: chosenResult.InlineMessageId!,
                        replyMarkup: updatedKeyboard
                    );
                }
            }
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
