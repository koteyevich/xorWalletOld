using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace xorWallet.Processors
{
    public static class StartProcessor
    {
        public static async Task ProcessStartAsync(Message message, TelegramBotClient bot)
        {
            var args = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (message.Chat.Type == ChatType.Private)
            {
                if (args is { Length: < 3 })
                {
                    if (args[1].StartsWith("Check_"))
                    {
                        var database = new Database();

                        var checkId = args[1].Replace("Check_", "");
                        var check = await database.GetCheckAsync(checkId);

                        if (check != null)
                        {
                            if (check.CheckOwnerUid == message.From?.Id)
                            {
                                throw new Exceptions.Message(
                                    "You are an owner of this check. Soon there will be an ability to revoke your checks.");
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
                        else
                        {
                            throw new Exceptions.Message("Check not found");
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
    }
}