using Telegram.Bot;

namespace xorWallet.BotFunctionality.Processors
{
    public abstract class BalanceProcessor
    {
        public static async Task BalanceAsync(long userId, TelegramBotClient bot)
        {
            var db = new Database();
            var user = await db.GetUserAsync(userId);

            await bot.SendMessage(userId, "Your balance is: " + user.Balance);
        }
    }
}
