using Telegram.Bot;
using Telegram.Bot.Types;

namespace xorWallet.Interfaces
{
    public interface ICallback
    {
        string Name { get; }
        string[] Aliases { get; }
        Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data);
    }
}
