using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.BotFunctionality.Processors;
using xorWallet.Interfaces;

namespace xorWallet.BotFunctionality.Commands
{
    public class Balance : CommandBase
    {
        public override string Name => "/balance";

        public override string Description => "Ваш баланс.";

        public override string[] Aliases => [];

        protected override async Task ExecuteCoreAsync(Message message, TelegramBotClient bot)
        {
            if (message.From != null) await BalanceProcessor.BalanceAsync(message.From.Id, bot);
        }
    }
}
