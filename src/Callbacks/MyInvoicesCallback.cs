using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Processors;

namespace xorWallet.Callbacks
{
    public class MyInvoicesCallback : ICallback
    {
        public string Name => "myInvoices";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            //! really risky, but it works. that processor won't be changed any time soon, so i'll give it a pass.
            var message = new Message()
            {
                Chat = new Chat()
                {
                    Id = callbackQuery.Message!.Chat.Id,
                },
                From = new User()
                {
                    Id = callbackQuery.From.Id,
                }
            };

            await MyInvoicesProcessor.MyInvoicesAsync(message, bot);
        }
    }
}
