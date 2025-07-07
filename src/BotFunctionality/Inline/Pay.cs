using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Interfaces;
using xorWallet.Utils;
using User = xorWallet.Models.User;

namespace xorWallet.BotFunctionality.Inline
{
    public class Pay : IInlineQuery
    {
        public string Name => "pay";

        public List<InlineQueryResultArticle> OnTyped(InlineQuery inlineQuery, TelegramBotClient bot,
            List<InlineQueryResultArticle> results, User user)
        {
            string cleanedQuery = inlineQuery.Query["pay".Length..].Trim();

            if (decimal.TryParse(cleanedQuery, out decimal decimalAmount))
            {
                int flooredAmount = (int)Math.Floor(decimalAmount);
                string messageText = $"Счёт на {flooredAmount} XOR";
                string description = "Нажми чтобы создать счёт.";

                if (decimalAmount != flooredAmount)
                {
                    description = $"⚠️ {decimalAmount} будет конвертировано в {flooredAmount}";
                }

                var inlineKeyboard = new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⏳ Создание Счёта",
                        $"null")
                );

                results.Add(
                    new InlineQueryResultArticle(
                        id: IdGenerator.GenerateId(),
                        title: $"Счёт на {flooredAmount} XOR",
                        inputMessageContent: new InputTextMessageContent(messageText)
                        {
                            ParseMode = ParseMode.Markdown
                        }
                    )
                    {
                        Description = description,
                        ReplyMarkup = inlineKeyboard
                    }
                );
            }
            else
            {
                results.Add(
                    new InlineQueryResultArticle(
                        id: IdGenerator.GenerateId(),
                        title: "Неверное количество",
                        inputMessageContent: new InputTextMessageContent(
                            "Неправильный формат. Попробуйте: @xorwallet_bot pay 10")
                    )
                    {
                        Description = "Пожалуйста, введите корректное количество после 'pay'"
                    }
                );
            }

            return results;
        }

        public async Task OnSelect(ChosenInlineResult chosenResult, TelegramBotClient bot)
        {
            string cleanedQuery = chosenResult.Query["pay".Length..].Trim();
            if (decimal.TryParse(cleanedQuery, out decimal decimalAmount))
            {
                int flooredAmount = (int)Math.Floor(decimalAmount);
                var db = new Database();

                string invoiceId = await db.CreateInvoiceAsync(chosenResult.From.Id, flooredAmount);

                var updatedKeyboard = new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl("💰 Оплатить счёт",
                        $"{StartUrlGenerator.GenerateStartUrl(invoiceId)}")
                );

                await bot.EditMessageReplyMarkup(
                    inlineMessageId: chosenResult.InlineMessageId!,
                    replyMarkup: updatedKeyboard
                );
            }
        }
    }
}
