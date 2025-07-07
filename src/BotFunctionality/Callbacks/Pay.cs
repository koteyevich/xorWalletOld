using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using Message = xorWallet.Exceptions.Message;

namespace xorWallet.Callbacks
{
    public class Pay : ICallback
    {
        public string Name => "pay";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            string[] splitData = data.Split('_');

            switch (splitData[1])
            {
                case "Invoice":
                    var database = new Database();

                    // get the invoice
                    string invoiceId = splitData[2];
                    var invoice = await database.GetInvoiceAsync(invoiceId);

                    if (invoice != null)
                    {
                        // get the users
                        var invoiceOwner = await database.GetUserAsync(invoice.InvoiceOwnerUid);
                        var payer = await database.GetUserAsync(callbackQuery.From.Id);

                        // update their balance
                        await database.UpdateBalanceAsync(payer.UserId, -invoice.Xors);
                        await database.UpdateBalanceAsync(invoiceOwner.UserId, invoice.Xors);

                        // cute little message that tells the owner that their invoice was paid, and tells the payer that he paid that invoice
                        invoiceOwner = await database.GetUserAsync(invoice.InvoiceOwnerUid);
                        payer = await database.GetUserAsync(callbackQuery.From.Id);
                        await bot.SendMessage(invoiceOwner.UserId,
                            $"💸 Счёт на {invoice.Xors} XOR был оплачен пользователем {(callbackQuery.From.Username != null ? $"@{callbackQuery.From.Username}" : callbackQuery.From.Id)}!\n" +
                            $"💰 Ваш новый баланс: {invoiceOwner.Balance} (+ {invoice.Xors} XOR)");
                        await bot.SendMessage(payer.UserId,
                            $"💵 Вы оплатили счёт на {invoice.Xors} XOR.\n" +
                            $"💰 Ваш новый баланс: {payer.Balance} XOR (- {invoice.Xors})");

                        await database.RemoveInvoiceAsync(invoiceId);
                        await bot.DeleteMessage(callbackQuery.From.Id, callbackQuery.Message!.MessageId);
                    }
                    else
                    {
                        throw new Message("Invoice not found");
                    }

                    break;
            }
        }
    }
}
