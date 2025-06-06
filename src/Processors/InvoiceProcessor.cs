using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace xorWallet.Processors
{
    public abstract class InvoiceProcessor
    {
        public static async Task InvoiceAsync(Message message, TelegramBotClient bot)
        {
            var db = new Database();
            string[] args = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 2)
            {
                throw new ArgumentException(
                    "Not enough arguments. Example usage: /invoice 10 (xors)");
            }

            long userId = message.From!.Id;
            int xors = int.Parse(args[1]);

            if (xors <= 0)
            {
                throw new ArgumentException("Xors must be greater than zero");
            }

            string invoice = await db.CreateInvoiceAsync(userId, xors);

            var botMessage = new StringBuilder();
            botMessage.AppendLine("Готово!");
            botMessage.AppendLine(
                $"Поделитесь этой ссылкой для оплаты: <code>https://t.me/xorwallet_bot?start={invoice}</code>");
            await bot.SendMessage(message.Chat.Id, botMessage.ToString(), ParseMode.Html);
        }
    }
}
