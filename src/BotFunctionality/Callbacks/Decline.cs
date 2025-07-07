using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;

namespace xorWallet.BotFunctionality.Callbacks
{
    public class Decline : ICallback
    {
        public string Name => "decline";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            await bot.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, "❎ Отклонено.");
        }
    }
}
