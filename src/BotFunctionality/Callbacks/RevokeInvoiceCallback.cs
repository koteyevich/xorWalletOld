using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using Message = xorWallet.Exceptions.Message;

namespace xorWallet.BotFunctionality.Callbacks
{
    public class RevokeInvoiceCallback : ICallback
    {
        public string Name => "revokeinvoice";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            string invoiceId = data.Split('_').Last();
            var db = new Database();

            var invoice = await db.GetInvoiceAsync(invoiceId);
            if (invoice == null)
            {
                throw new Message("Invoice not found");
            }

            await db.RemoveInvoiceAsync(invoiceId);

            await bot.SendMessage(callbackQuery.Message!.Chat.Id, $"☑️ Готово! \n" +
                                                                  $"📊 Счёт {invoiceId} был отозван.");
        }
    }
}
