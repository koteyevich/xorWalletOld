using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using xorWallet.Utils;


namespace xorWallet.Processors
{
    public static class StartProcessor
    {
        public static async Task ProcessStartAsync(Message message, TelegramBotClient bot)
        {
            Logger.Command("Processing /start", "INFO");
            var args = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (message.Chat.Type == ChatType.Private)
            {
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