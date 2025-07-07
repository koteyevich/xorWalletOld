using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Processors;

namespace xorWallet.Commands
{
    public class Invoice : CommandBase
    {
        public override string Name => "/invoice";

        public override string Description => "Создаёт счёт.";

        public override string[] Aliases => [];

        protected override async Task ExecuteCoreAsync(Message message, TelegramBotClient bot)
        {
            await InvoiceProcessor.InvoiceAsync(message, bot);
        }
    }
}
