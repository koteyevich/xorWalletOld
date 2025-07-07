using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Utils;

namespace xorWallet.BotFunctionality.Processors
{
    public abstract class CheckProcessor
    {
        /// <summary>
        /// Command that is used to generate checks. Checks need 2 things: number of XORs, and number of activations.
        /// If those parameters are given and correct, generate the check.
        /// After the creation on the backend, the message about success is sent.
        /// </summary>
        /// <param name="message">Used to get the userID and chatID. Also used to get the message text to split it into arguments that are used when creating the check.</param>
        /// <param name="bot">Used for sending messages.</param>
        /// <exception cref="Message">(can't reference the correct message...) If the arguments are incorrect.</exception>
        public static async Task CheckAsync(Message message, TelegramBotClient bot)
        {
            var db = new Database();
            string[] args = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 3)
            {
                throw new Exceptions.Message(
                    "Не достаточно аргументов. Пример: /check 10 (xors) 4 (количество активаций)");
            }

            long userId = message.From!.Id;
            int xors = int.Parse(args[1]);
            int activations = int.Parse(args[2]);

            if (activations <= 0)
            {
                throw new Exceptions.Message("Активации должны быть > 0");
            }

            if (xors <= 0)
            {
                throw new Exceptions.Message("XOR должны быть > 0");
            }

            string check = await db.CreateCheckAsync(userId, xors, activations);
            var user = await db.GetUserAsync(userId);

            var botMessage = new StringBuilder();
            botMessage.AppendLine("✅ Готово!");
            botMessage.AppendLine(
                $"➡️ Поделитесь этой ссылкой для активации: <code>{StartUrlGenerator.GenerateStartUrl(check)}</code>");
            botMessage.AppendLine($"💰 Ваш новый баланс: {user.Balance} XOR (- {xors * activations})");

            var keyboard = new InlineKeyboardMarkup();
            var qrButton = EncryptedInlineButton.InlineButton("🔳 QR", $"qr_{check}");

            keyboard.AddButton(qrButton);

            await bot.SendMessage(message.Chat.Id, botMessage.ToString(), ParseMode.Html, replyMarkup: keyboard);
        }
    }
}
