using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Utils;

namespace xorWallet.BotFunctionality.Callbacks
{
    public class CallbackRegistry
    {
        private readonly Dictionary<string, ICallback> callbacks = new();

        public CallbackRegistry()
        {
            var callbackList = new List<ICallback>
            {
                new RevokeCheckCallback(),
                new RevokeInvoiceCallback(),
                new Decline(),
                new Pay(),
                new MyChecksCallback(),
                new MyInvoicesCallback(),
                new CreateCheck(),
                new CreateInvoice(),
                new QrCallback(),
                new DepositStarsCallback()
            };

            foreach (var cb in callbackList)
            {
                callbacks[cb.Name.ToLower()] = cb;
                foreach (string alias in cb.Aliases)
                {
                    callbacks[alias.ToLower()] = cb;
                }
            }
        }

        /// <summary>
        /// Decrypts the callback, then tries to match with the <see cref="callbacks"/>.
        /// <c>null</c> callback is used for buttons that display text, but have no function.
        /// </summary>
        /// <param name="query">Used to get the query data</param>
        /// <param name="bot">Used to answer the queries</param>
        public async Task HandleCallbackAsync(CallbackQuery query, TelegramBotClient bot)
        {
            if (string.IsNullOrWhiteSpace(query.Data)) return;

            if (query.Data == "null")
            {
                await bot.AnswerCallbackQuery(query.Id);
                return;
            }

            string? decrypted = Encryption.DecryptCallback(query.Data);
            if (string.IsNullOrWhiteSpace(decrypted)) return;

            Logger.Log($"Decrypted callback: {decrypted}", "CALLBACK", "DEBUG");

            string key = decrypted.Split('_')[0].ToLower();

            if (callbacks.TryGetValue(key, out var handler))
            {
                await handler.ExecuteAsync(query, bot, decrypted);
                await bot.AnswerCallbackQuery(query.Id);
            }
            else
            {
                await bot.AnswerCallbackQuery(query.Id, "Неизвестная кнопка.");
            }
        }
    }
}
