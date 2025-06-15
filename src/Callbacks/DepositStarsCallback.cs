using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
            // yanderedev ahh code
            switch (data)
            {
                case "depositStars":
                    var keyboard = new InlineKeyboardMarkup();

                    var buttonPay15 = EncryptedInlineButton.InlineButton("15 XOR️️ (10 XTR)", "depositStars15");
                    buttonPay15.Pay = true;
                    var buttonPay30 = EncryptedInlineButton.InlineButton("30 XOR (20 XTR)", "depositStars30");
                    buttonPay30.Pay = true;
                    var buttonPay50 = EncryptedInlineButton.InlineButton("50 XOR (35 XTR)", "depositStars50");
                    buttonPay50.Pay = true;

                    keyboard.AddButtons(buttonPay15, buttonPay30, buttonPay50);
                    await bot.SendMessage(callbackQuery.Message!.Chat.Id, "Выберите сумму пополнения. \n" +
                                                                          "⚠️ <b>ВАЖНО:</b> <i>XOR — это внутренняя валюта бота, которая НЕ <b>ИМЕЕТ РЕАЛЬНОЙ ЦЕННОСТИ</b>. Пополнение счёта является исключительно добровольным пожертвованием. <b>Вы НЕ сможете вывести деньги или обменять XOR на реальные средства.</b> Пополняя счёт, вы соглашаетесь с этими условиями.</i>",
                        parseMode: ParseMode.Html,
                        replyMarkup: keyboard);
                    break;
                case "depositStars15":
                    await bot.SendInvoice(
                        chatId: callbackQuery.Message!.Chat.Id,
                        title: "Купить 15 XOR",
                        description: "Покупка 15 XOR за 10 звёзд",
                        payload: "purchase-15-xor",
                        currency: "XTR",
                        prices: new List<LabeledPrice>
                        {
                            new("15 XOR (10 ⭐️)",
                                10),
                        }
                    );
                    break;
                case "depositStars30":
                    await bot.SendInvoice(
                        chatId: callbackQuery.Message!.Chat.Id,
                        title: "Купить 30 XOR",
                        description: "Покупка 30 XOR за 20 звёзд",
                        payload: "purchase-30-xor",
                        currency: "XTR",
                        prices: new List<LabeledPrice>
                        {
                            new("30 XOR (20 ⭐️)",
                                20),
                        }
                    );
                    break;
                case "depositStars50":
                    await bot.SendInvoice(
                        chatId: callbackQuery.Message!.Chat.Id,
                        title: "Купить 50 XOR",
                        description: "Покупка 50 XOR за 35 звёзд",
                        payload: "purchase-50-xor",
                        currency: "XTR",
                        prices: new List<LabeledPrice>
                        {
                            new("50 XOR (35 ⭐️)",
                                35),
                        }
                    );
                    break;
            }
        }
    }
}
