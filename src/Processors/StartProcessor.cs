using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Models;
using xorWallet.Utils;

namespace xorWallet.Processors
{
    public static class StartProcessor
    {
        public static async Task ProcessStartAsync(Message message, TelegramBotClient bot)
        {
            string[]? args = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (message.Chat.Type == ChatType.Private)
            {
                if (args is { Length: > 1 })
                {
                    if (args[1].StartsWith("Check_"))
                    {
                        var database = new Database();

                        string checkId = args[1].Replace("Check_", "");
                        var check = await database.GetCheckAsync(checkId);

                        if (check != null)
                        {
                            await checkActivation(message, bot, check, database);
                        }
                        else
                        {
                            throw new Exceptions.Message("Check not found");
                        }
                    }

                    if (args[1].StartsWith("Invoice_"))
                    {
                        var database = new Database();

                        string invoiceId = args[1].Replace("Invoice_", "");
                        var invoice = await database.GetInvoiceAsync(invoiceId);

                        if (invoice != null)
                        {
                            await invoiceActivation(message, bot, invoice);
                        }
                        else
                        {
                            throw new Exceptions.Message("Invoice not found");
                        }
                    }

                    return;
                }

                var db = new Database();
                var user = await db.GetUserAsync(message.From.Id);

                var keyboard = new InlineKeyboardMarkup();
                var balanceButton = new InlineKeyboardButton($"💰 Баланс: {user.Balance} XOR", "null");
                keyboard.AddButton(balanceButton);

                var myChecksButton = EncryptedInlineButton.InlineButton("📋 Мои чеки", "myChecks");
                var myInvoicesButton = EncryptedInlineButton.InlineButton("📊 Мои счета", "myInvoices");
                keyboard.AddNewRow(myChecksButton, myInvoicesButton);

                var createCheckButton = EncryptedInlineButton.InlineButton("🧾 Создать чек", "createCheck");
                var createInvoiceButton = EncryptedInlineButton.InlineButton("🧾 Создать счёт", "createInvoice");
                keyboard.AddNewRow(createCheckButton, createInvoiceButton);

                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "👋 Добро пожаловать в xorWallet.\n" +
                          "<i><u>ℹ️ Помните что вся валюта вымышлена и бесценна.</u></i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true }
                );
            }
        }

        private static async Task invoiceActivation(Message message, TelegramBotClient bot, Invoice invoice)
        {
            if (invoice.InvoiceOwnerUid == message.From?.Id)
            {
                await invoiceOwner(message, bot, invoice);
                return;
            }

            var keyboard = new InlineKeyboardMarkup();
            var acceptButton =
                EncryptedInlineButton.InlineButton($"✅ Оплатить {invoice.Xors} XOR", $"pay_Invoice_{invoice.Id}");
            var rejectButton = EncryptedInlineButton.InlineButton("❎ Отклонить", "decline");

            keyboard.AddButton(acceptButton);
            keyboard.AddNewRow(rejectButton);

            await bot.SendMessage(message.Chat.Id, $"💰 Счёт на {invoice.Xors} XOR", replyMarkup: keyboard);
        }

        private static async Task checkActivation(Message message, TelegramBotClient bot, Check check,
            Database database)
        {
            if (check.CheckOwnerUid == message.From?.Id)
            {
                await checkOwner(message, bot, check);
                return;
            }

            if (check.UserActivated.Any(uid => uid == message.From?.Id))
            {
                throw new Exceptions.Message(
                    "Ты уже активировал чек! Оставь немного другим...");
            }

            await database.UpdateCheckAsync(check, message.From!.Id);

            var user = await database.GetUserAsync(message.From.Id);
            await bot.SendMessage(message.Chat.Id, $"✅ Готово!\n💰 Новый баланс: {user.Balance} XOR (+ {check.Xors})");
        }

        private static async Task invoiceOwner(Message message, TelegramBotClient bot, Invoice invoice)
        {
            var keyboard = new InlineKeyboardMarkup();
            var revokeCheckButton =
                EncryptedInlineButton.InlineButton("⬅️ Отозвать счёт", $"revokeinvoice_{invoice.Id}");
            var qrButton = EncryptedInlineButton.InlineButton("🔳 QR", $"qr_Invoice_{invoice.Id}");

            keyboard.AddButtons(revokeCheckButton, qrButton);

            await bot.SendMessage(message.Chat.Id,
                $"📊 Это ваш счёт, вы можете его отозвать.\n" +
                $"<b>Вы точно хотите отозвать счёт на {invoice.Xors} XOR?</b>\n\n" +
                $"Если же не хотите отзывать, то можете поделиться чеком.\n" +
                $"<code>{StartUrlGenerator.GenerateStartUrl($"Invoice_{invoice.Id}")}</code>",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }

        private static async Task checkOwner(Message message, TelegramBotClient bot, Check check)
        {
            var keyboard = new InlineKeyboardMarkup();
            var revokeCheckButton =
                EncryptedInlineButton.InlineButton("⬅️ Отозвать чек", $"revokecheck_{check.Id}");
            var qrButton = EncryptedInlineButton.InlineButton("🔳 QR", $"qr_Check_{check.Id}");

            keyboard.AddButtons(revokeCheckButton, qrButton);

            await bot.SendMessage(message.Chat.Id,
                $"📋 Это ваш чек, вы можете его отозвать.\n" +
                $"Осталось активаций: {check.Activations}\n" +
                $"Если вы сейчас отзовёте чек, то вернёте себе <b>{check.Activations * check.Xors}</b> XOR\n\n" +
                $"Если же не хотите отзывать, то можете поделиться чеком.\n" +
                $"<code>{StartUrlGenerator.GenerateStartUrl($"Check_{check.Id}")}</code>",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard);
        }
    }
}
