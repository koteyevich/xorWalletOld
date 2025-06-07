using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Utils;

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
                    "Не достаточно аргументов. Пример: /invoice 10 (xors)");
            }

            long userId = message.From!.Id;
            int xors = int.Parse(args[1]);

            if (xors <= 0)
            {
                throw new ArgumentException("XOR должны быть > 0");
            }

            string invoice = await db.CreateInvoiceAsync(userId, xors);

            var botMessage = new StringBuilder();
            botMessage.AppendLine("✅ Готово!");
            botMessage.AppendLine(
                $"➡️ Поделитесь этой ссылкой для оплаты: <code>https://t.me/xorwallet_bot?start={invoice}</code>");

            var keyboard = new InlineKeyboardMarkup();
            var qrButton = EncryptedInlineButton.InlineButton("🔳 QR", $"qr_{invoice}");

            keyboard.AddButton(qrButton);

            await bot.SendMessage(message.Chat.Id, botMessage.ToString(), ParseMode.Html, replyMarkup: keyboard);
        }
    }
}
