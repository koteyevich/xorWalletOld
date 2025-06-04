using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace xorWallet.Processors;

public abstract class CheckProcessor
{
    public static async Task CheckAsync(Message message, TelegramBotClient bot)
    {
        var db = new Database();
        var args = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (args.Length < 3)
        {
            throw new ArgumentException(
                "Not enough arguments. Example usage: /check 10 (xors) 4 (amount of activations)");
        }

        var userId = message.From.Id;
        var xors = int.Parse(args[1]);
        var activations = int.Parse(args[2]);

        if (activations <= 0)
        {
            throw new ArgumentException("Activations must be greater than zero");
        }

        if (xors <= 0)
        {
            throw new ArgumentException("Xors must be greater than zero");
        }

        var check = await db.CreateCheckAsync(userId, xors, activations);
        var user = await db.GetUserAsync(userId);

        var botMessage = new StringBuilder();
        botMessage.AppendLine("Готово!");
        botMessage.AppendLine(
            $"Поделитесь этой ссылкой для активации: <code>https://t.me/xorwallet_bot?start={check}</code>");
        botMessage.AppendLine($"Ваш новый баланс: {user.Balance}");
        await bot.SendMessage(message.Chat.Id, botMessage.ToString(), ParseMode.Html);
    }
}