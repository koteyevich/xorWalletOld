using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using User = xorWallet.Models.User;

namespace xorWallet.Interfaces
{
    public interface IInlineQuery
    {
        string Name { get; }

        List<InlineQueryResultArticle> OnTyped(InlineQuery inlineQuery, TelegramBotClient bot,
            List<InlineQueryResultArticle> results, User user);

        Task OnSelect(ChosenInlineResult chosenResult, TelegramBotClient bot);
    }
}
