using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Utils;

namespace xorWallet.Commands.Debug
{
    public class TestDebugCommand : DebugCommandBase
    {
        public override string Name => "/debug_test";
        public override string Description => "Тест-команда.";
        public override bool RequiresDeveloper => true;
        public override bool RequiresAdmin => false;

        protected override async Task ExecuteCoreAsync(Message message, TelegramBotClient bot)
        {
            try
            {
                await bot.SendMessage(message.Chat.Id, "hello!");
            }
            catch (Exception ex)
            {
                Logger.Command($"Debug status failed: {ex.Message}", "ERROR");
            }
        }
    }
}
