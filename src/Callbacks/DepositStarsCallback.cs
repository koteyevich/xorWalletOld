using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Interfaces;
using xorWallet.Utils;

namespace xorWallet.Callbacks
{
    public class DepositStarsCallback : ICallback
    {
        public string Name => "depositStars";

        public string[] Aliases => ["depositStars15", "depositStars30", "depositStars50"];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            switch (data)
            {
                case "depositStars":
                    var keyboard = new InlineKeyboardMarkup();

                    var buttonPay15 = EncryptedInlineButton.InlineButton("15 ⭐️️", "depositStars15");
                    buttonPay15.Pay = true;
                    var buttonPay30 = EncryptedInlineButton.InlineButton("30 ⭐️️", "depositStars30");
                    buttonPay30.Pay = true;
                    var buttonPay50 = EncryptedInlineButton.InlineButton("50 ⭐️️", "depositStars50");
                    buttonPay50.Pay = true;

                    keyboard.AddButtons(buttonPay15, buttonPay30, buttonPay50);
                    await bot.SendMessage(callbackQuery.Message.Chat.Id, "выбери", replyMarkup: keyboard);
                    break;
                case "depositStars15":
                    await bot.SendInvoice(
                        chatId: callbackQuery.Message.Chat.Id,
                        title: "Купить 15 XOR",
                        description: "Покупка 15 XOR за 15 звёзд",
                        payload: "purchase-15-xor",
                        currency: "XTR",
                        prices: new List<LabeledPrice>
                        {
                            new("15 XOR (15 ⭐️)",
                                15),
                        }
                    );
                    break;
                case "depositStars30":
                    await bot.SendInvoice(
                        chatId: callbackQuery.Message.Chat.Id,
                        title: "Купить 30 XOR",
                        description: "Покупка 30 XOR за 30 звёзд",
                        payload: "purchase-30-xor",
                        currency: "XTR",
                        prices: new List<LabeledPrice>
                        {
                            new("30 XOR (30 ⭐️)",
                                30),
                        }
                    );
                    break;
                case "depositStars50":
                    await bot.SendInvoice(
                        chatId: callbackQuery.Message.Chat.Id,
                        title: "Купить 50 XOR",
                        description: "Покупка 50 XOR за 50 звёзд",
                        payload: "purchase-50-xor",
                        currency: "XTR",
                        prices: new List<LabeledPrice>
                        {
                            new("50 XOR (50 ⭐️)",
                                50),
                        }
                    );
                    break;
            }
        }
    }
}
