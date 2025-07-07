using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Interfaces;

namespace xorWallet.Callbacks
{
    public class CreateInvoice : ICallback
    {
        public string Name => "createInvoice";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            await bot.SendMessage(callbackQuery.Message!.Chat.Id,
                "💬 Введите количество XOR (пример: 10)", replyMarkup: new ForceReplyMarkup());
        }
    }
}
