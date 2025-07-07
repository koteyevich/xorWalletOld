using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace xorWallet.BotFunctionality.Processors
{
    public class MyInvoicesProcessor
    {
        public static async Task MyInvoicesAsync(Message message, TelegramBotClient bot)
        {
            var database = new Database();
            await bot.SendMessage(
                message.Chat.Id,
                (await database.ListUserInvoices(message.From!.Id)).ToString(),
                ParseMode.Html
            );
        }
    }
}
