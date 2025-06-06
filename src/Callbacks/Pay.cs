using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;

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

                    string invoiceId = splitData[2];
                    var invoice = await database.GetInvoiceAsync(invoiceId);

                    if (invoice != null)
                    {
                        var invoiceOwner = await database.GetUserAsync(invoice.InvoiceOwnerUid);
                        var payer = await database.GetUserAsync(callbackQuery.From.Id);

                        await database.UpdateBalanceAsync(payer.UserId, -invoice.Xors);
                        await database.UpdateBalanceAsync(invoiceOwner.UserId, invoice.Xors);

                        invoiceOwner = await database.GetUserAsync(invoice.InvoiceOwnerUid);
                        payer = await database.GetUserAsync(callbackQuery.From.Id);
                        await bot.SendMessage(invoiceOwner.UserId,
                            $"Счёт на {invoice.Xors} XOR был оплачен пользователем {(callbackQuery.From.Username != null ? $"@{callbackQuery.From.Username}" : callbackQuery.From.Id)}!\n" +
                            $"Ваш новый баланс: {invoiceOwner.Balance} (+ {invoice.Xors} xor'ов)");
                        await bot.SendMessage(payer.UserId,
                            $"Вы оплатили счёт на {invoice.Xors} XOR.\n" +
                            $"Ваш новый баланс: {payer.Balance}");

                        await database.RemoveInvoiceAsync(invoiceId);
                        await bot.DeleteMessage(callbackQuery.From.Id, callbackQuery.Message!.MessageId);
                    }
                    else
                    {
                        throw new Exception("Invoice not found");
                    }

                    break;
            }
        }
    }
}
