using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.BotFunctionality.Processors;
using xorWallet.Interfaces;

namespace xorWallet.BotFunctionality.Commands
{
    public class Check : CommandBase
    {
        public override string Name => "/Check";

        public override string Description => "Создать чек.";

        public override string[] Aliases => [];

        protected override async Task ExecuteCoreAsync(Message message, TelegramBotClient bot)
        {
            await CheckProcessor.CheckAsync(message, bot);
        }
    }
}
