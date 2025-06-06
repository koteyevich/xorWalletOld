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
                            await invoiceActivation(message, bot, invoice, database);
                        }
                        else
                        {
                            throw new Exceptions.Message("Invoice not found");
                        }
                    }

                    return;
                }

                // this should be last after all the checks
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Добро пожаловать в xorWallet.\n" +
                          "Помните что вся валюта вымышлена и бесценна.",
                    parseMode: ParseMode.Html,
                    linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true }
                );
            }
        }

        private static async Task invoiceActivation(Message message, TelegramBotClient bot, Invoice invoice,
            Database database)
        {
            if (invoice.InvoiceOwnerUid == message.From?.Id)
            {
                await invoiceOwner(message, bot, invoice);
                return;
            }

            var keyboard = new InlineKeyboardMarkup();
            var acceptButton =
                EncryptedInlineButton.InlineButton($"Оплатить {invoice.Xors} xor", $"pay_Invoice_{invoice.Id}");
            var rejectButton = EncryptedInlineButton.InlineButton("Отклонить", "decline");

            keyboard.AddButton(acceptButton);
            keyboard.AddNewRow(rejectButton);

            await bot.SendMessage(message.Chat.Id, $"Чек на {invoice.Xors} xor'ов", replyMarkup: keyboard);
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
                    "You've already activated this check! Leave some for others...");
            }

            await database.UpdateCheckAsync(check, message.From!.Id);

            var user = await database.GetUserAsync(message.From.Id);
            await bot.SendMessage(message.Chat.Id, $"Готово!\nНовый баланс: {user.Balance} xor'ов");
        }

        private static async Task invoiceOwner(Message message, TelegramBotClient bot, Invoice invoice)
        {
            var keyboard = new InlineKeyboardMarkup();
            var revokeCheckButton = EncryptedInlineButton.InlineButton("Отозвать счёт", $"revokeinvoice_{invoice.Id}");

            keyboard.AddButton(revokeCheckButton);

            await bot.SendMessage(message.Chat.Id,
                $"Это ваш счёт, вы можете его отозвать.\n" +
                $"Вы точно хотите отозвать чек на {invoice.Xors} xor'ов?", replyMarkup: keyboard);
        }

        private static async Task checkOwner(Message message, TelegramBotClient bot, Check check)
        {
            var keyboard = new InlineKeyboardMarkup();
            var revokeCheckButton =
                EncryptedInlineButton.InlineButton("Отозвать чек", $"revokecheck_{check.Id}");

            keyboard.AddButton(revokeCheckButton);

            await bot.SendMessage(message.Chat.Id,
                $"Это ваш чек, вы можете его отозвать.\n" +
                $"Осталось активаций: {check.Activations}\n" +
                $"Если вы сейчас отзовёте чек, то вернёте себе {check.Activations * check.Xors} xor'ов",
                replyMarkup: keyboard);
        }
    }
}
