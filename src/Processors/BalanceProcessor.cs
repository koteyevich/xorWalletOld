using Telegram.Bot;
using xorWallet.Exceptions;
using xorWallet.Utils;

namespace xorWallet.Processors;

public abstract class BalanceProcessor
{
    public static async Task BalanceAsync(long userId, TelegramBotClient bot)
    {
        try
        {
            var db = new Database();

            var user = await db.GetUserAsync(userId);
            if (user == null)
            {
                await db.CreateUserAsync(userId);
                user = await db.GetUserAsync(userId);
            }

            await bot.SendMessage(userId, "Your balance is: " + user?.Balance);
        }
        catch (BotException e)
        {
            Logger.Error($"shit hit the fan in balance: {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            Logger.Error($"shit hit the fan balance: {e.Message}");
            throw;
        }
    }
}