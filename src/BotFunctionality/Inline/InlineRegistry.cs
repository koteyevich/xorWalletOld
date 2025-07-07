using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using xorWallet.Interfaces;
using xorWallet.Utils;
using User = xorWallet.Models.User;

namespace xorWallet.BotFunctionality.Inline
{
    public class InlineRegistry
    {
        private readonly Dictionary<string, IInlineQuery> inlines = new();

        public InlineRegistry()
        {
            var inlineList = new List<IInlineQuery>
            {
                new Pay(),
                new Give()
            };

            foreach (var i in inlineList)
            {
                inlines[i.Name.ToLower()] = i;
            }
        }

        public async Task<List<InlineQueryResultArticle>> OnInlineQueryTyped(InlineQuery query, TelegramBotClient bot)
        {
            Console.WriteLine($"OnInlineQuery called: Query={query.Query}, From={query.From.Username}, Id={query.Id}");
            var results = new List<InlineQueryResultArticle>();

            var db = new Database();
            var user = await db.GetUserAsync(query.From.Id);

            // if no query is typed, just output current balance with a little help description
            if (string.IsNullOrWhiteSpace(query.Query))
            {
                results.Add(
                    new InlineQueryResultArticle(
                        id: IdGenerator.GenerateId(),
                        title: $"Ваш баланс: {user.Balance} XOR",
                        inputMessageContent: new InputTextMessageContent($"Мой баланс: {user.Balance} XOR")
                        {
                            ParseMode = ParseMode.Markdown
                        }
                    )
                    {
                        Description = "Ваш текущий баланс",
                    }
                );

                results.Add(new InlineQueryResultArticle(
                    id: IdGenerator.GenerateId(),
                    title: $"Помощь", new InputTextMessageContent($"Помощь"))
                {
                    Description = "@xorwallet_bot pay 15 - создать счёт, @xorwallet_bot give 5 3 - создать чек"
                });
            }
            else
            {
                string key = query.Query.Split(' ')[0].ToLower();

                if (inlines.TryGetValue(key, out var handler))
                {
                    results = handler.OnTyped(query, bot, results, user);
                }
            }

            return results;
        }

        public async Task OnInlineChosen(TelegramBotClient bot, ChosenInlineResult chosenResult)
        {
            Console.WriteLine(
                $"User {chosenResult.From.Username} chose result: {chosenResult.ResultId}, Query: {chosenResult.Query}");

            string key = chosenResult.Query.Split(' ')[0].ToLower();

            if (inlines.TryGetValue(key, out var handler))
            {
                await handler.OnSelect(chosenResult, bot);
            }
        }
    }
}
