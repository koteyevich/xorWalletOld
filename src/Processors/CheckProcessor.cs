using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Utils;

namespace xorWallet.Processors
{
    public abstract class CheckProcessor
    {
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
