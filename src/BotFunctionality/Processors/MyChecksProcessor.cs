using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace xorWallet.BotFunctionality.Processors
{
    public class MyChecksProcessor
    {
        public static async Task MyChecksAsync(Message message, TelegramBotClient bot)
        {
            var database = new Database();
            await bot.SendMessage(
                message.Chat.Id,
                (await database.ListUserChecks(message.From!.Id)).ToString(),
                ParseMode.Html
            );
        }
    }
}
