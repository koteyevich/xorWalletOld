using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using Message = xorWallet.Exceptions.Message;

namespace xorWallet.Callbacks
{
    public class RevokeCheckCallback : ICallback
    {
        public string Name => "revokecheck";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            string checkId = data.Split('_').Last();
            var db = new Database();

            var check = await db.GetCheckAsync(checkId);
            if (check == null)
            {
                throw new Message("Check not found");
            }

            await db.UpdateBalanceAsync(check.CheckOwnerUid, check.Activations * check.Xors);
            await db.RemoveCheckAsync(checkId);
            var user = await db.GetUserAsync(callbackQuery.From.Id);

            await bot.SendMessage(callbackQuery.Message!.Chat.Id, $"☑️ Готово! \n" +
                                                                  $"📋 Чек {checkId} был отозван. Ваш новый баланс: {user.Balance} XOR (+ {check.Activations * check.Xors})");
        }
    }
}
