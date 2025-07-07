using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Utils;

namespace xorWallet.BotFunctionality.Processors
{
    public abstract class InvoiceProcessor
    {
        /// <summary>
        /// Command that is used to generate invoices. Invoices need 1 thing: number of XORs.
        /// If those parameters are given and correct, generate the invoice.
        /// After the creation on the backend, send a message about success.
        /// </summary>
        /// <param name="message">Used to get the userID and chatID. Also used to get the message text to split it into arguments that are used when creating the invoice.</param>
        /// <param name="bot">Used for sending messages.</param>
        /// <exception cref="Message">(can't reference the correct message...) if the arguments are incorrect.</exception>
        public static async Task InvoiceAsync(Message message, TelegramBotClient bot)
        {
            var db = new Database();
            string[] args = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 2)
            {
                throw new Exceptions.Message(
                    "Не достаточно аргументов. Пример: /invoice 10 (xors)");
            }

            long userId = message.From!.Id;
            int xors = int.Parse(args[1]);

            if (xors <= 0)
            {
                throw new Exceptions.Message("XOR должны быть > 0");
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
