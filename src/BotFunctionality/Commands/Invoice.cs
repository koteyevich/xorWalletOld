using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.BotFunctionality.Processors;
using xorWallet.Interfaces;

namespace xorWallet.BotFunctionality.Commands
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
