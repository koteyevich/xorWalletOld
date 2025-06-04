using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Processors;

namespace xorWallet.Commands
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
