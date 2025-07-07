using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.BotFunctionality.Processors;
using xorWallet.Interfaces;

namespace xorWallet.BotFunctionality.Commands
{
    public class Start : CommandBase
    {
        public override string Name => "/start";
        public override string Description => "Запускает бота.";

        public override string[] Aliases => [];

        protected override async Task ExecuteCoreAsync(Message message, TelegramBotClient bot)
        {
            await StartProcessor.ProcessStartAsync(message, bot);
        }
    }
}
