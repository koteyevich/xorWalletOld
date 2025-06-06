using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Utils;

namespace xorWallet.Callbacks
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

        public async Task HandleCallbackAsync(CallbackQuery query, TelegramBotClient bot)
        {
            if (string.IsNullOrWhiteSpace(query.Data)) return;

            string? decrypted = Encryption.DecryptCallback(query.Data);
            if (string.IsNullOrWhiteSpace(decrypted)) return;

            Logger.Log($"Decrypted callback: {decrypted}", "CALLBACK", "DEBUG");

            string key = decrypted.Split('_')[0].ToLower();

            if (callbacks.TryGetValue(key, out var handler))
            {
                await handler.ExecuteAsync(query, bot, decrypted);
            }
            else
            {
                if (query.Data == "null")
                    return;

                await bot.AnswerCallbackQuery(query.Id, "Неизвестная кнопка.");
            }
        }
    }
}
