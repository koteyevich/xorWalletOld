using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Processors;

namespace xorWallet.Commands
{
    public class MyInvoices : CommandBase
    {
        public override string Name => "/myinvoices";

        public override string Description => "Лист счетов, которые вы создали.";

        public override string[] Aliases => [];

        protected override async Task ExecuteCoreAsync(Message message, TelegramBotClient bot)
        {
            await MyInvoicesProcessor.MyInvoicesAsync(message, bot);
        }
    }
}
