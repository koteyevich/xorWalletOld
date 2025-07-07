using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using xorWallet.Interfaces;
using xorWallet.Utils;
using static System.Decimal;
using User = xorWallet.Models.User;

namespace xorWallet.BotFunctionality.Inline
{
    public class Give : IInlineQuery
    {
        public string Name => "give";

        public List<InlineQueryResultArticle> OnTyped(InlineQuery inlineQuery, TelegramBotClient bot,
            List<InlineQueryResultArticle> results,
            User user)
        {
            string[] cleanedQuery = inlineQuery.Query.Replace("give", string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (TryParse(cleanedQuery[0], out decimal xorAmount))
            {
                TryParse(cleanedQuery[1], out decimal activationAmount);

                int flooredXorAmount = (int)Math.Floor(xorAmount);
                int flooredActivationAmount = (int)Math.Floor(activationAmount);
                string messageText = $"Чек на {flooredXorAmount} XOR ({flooredActivationAmount} акт.)";
                string description = "Нажми чтобы создать чек.";

                if (user.Balance < flooredXorAmount * flooredActivationAmount)
                {
                    description = $"Нужно {flooredXorAmount * flooredActivationAmount} XOR для чека.";
                }

                if (xorAmount != flooredXorAmount)
                {
                    description = $"⚠️ {xorAmount} будет конвертировано в {flooredXorAmount}\n";
                }

                if (activationAmount != flooredActivationAmount)
                {
                    description += $"{activationAmount} будет конвертировано в {flooredActivationAmount}\n";
                }

                var inlineKeyboard = new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⏳ Создание Чека",
                        $"null")
                );

                if (user.Balance < flooredXorAmount * flooredActivationAmount)
                {
                    results.Add(
                        new InlineQueryResultArticle(
                            id: IdGenerator.GenerateId(),
                            title: "Недостаточно XOR.",
                            inputMessageContent: new InputTextMessageContent(
                                $"Недостаточно XOR.\nВы создаёте чек на: {flooredXorAmount * flooredActivationAmount} ({flooredXorAmount} * {flooredActivationAmount})\nВаш баланс: {user.Balance}")
                        )
                        {
                            Description = description,
                        }
                    );
                }
                else
                {
                    results.Add(
                        new InlineQueryResultArticle(
                            id: IdGenerator.GenerateId(),
                            title: $"Чек на {flooredXorAmount} XOR ({activationAmount} активаций)",
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
            }
            else
            {
                results.Add(
                    new InlineQueryResultArticle(
                        id: IdGenerator.GenerateId(),
                        title: "Неверное количество",
                        inputMessageContent: new InputTextMessageContent(
                            "Неправильный формат. Попробуйте: @xorwallet_bot give 3 5")
                    )
                    {
                        Description = "Пожалуйста, введите корректное количество после 'give'"
                    }
                );
            }

            return results;
        }

        public async Task OnSelect(ChosenInlineResult chosenResult, TelegramBotClient bot)
        {
            string[] cleanedQuery = chosenResult.Query.Replace("give", string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (TryParse(cleanedQuery[0], out decimal decimalXor))
            {
                TryParse(cleanedQuery[1], out decimal activationAmount);

                int flooredAmount = (int)Math.Floor(decimalXor);
                int flooredActivationAmount = (int)Math.Floor(activationAmount);
                var db = new Database();

                string checkId =
                    await db.CreateCheckAsync(chosenResult.From.Id, flooredAmount, flooredActivationAmount);

                var updatedKeyboard = new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl("💰 Активировать чек",
                        $"{StartUrlGenerator.GenerateStartUrl(checkId)}")
                );

                await bot.EditMessageReplyMarkup(
                    inlineMessageId: chosenResult.InlineMessageId!,
                    replyMarkup: updatedKeyboard
                );
            }
        }
    }
}
