using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Interfaces;

namespace xorWallet.Callbacks
{
    public class CreateCheck : ICallback
    {
        public string Name => "createCheck";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            await bot.SendMessage(callbackQuery.Message.Chat.Id,
                $"Введите количество XOR и количество активаций (пример: 5 3)", replyMarkup: new ForceReplyMarkup()
                {
                });
        }
    }
}
