using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Utils;

namespace xorWallet.Interfaces
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        string[] Aliases { get; }
        Task ExecuteAsync(Message message, TelegramBotClient bot);
        bool RequiresAdmin { get; }
    }

    public abstract class CommandBase : ICommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string[] Aliases { get; }
        public virtual bool RequiresAdmin => true;

        public async Task ExecuteAsync(Message message, TelegramBotClient bot)
        {
            await ExecuteCoreAsync(message, bot);
            Logger.Command($"Processing {Name} command.");
        }

        protected abstract Task ExecuteCoreAsync(Message message, TelegramBotClient bot);
    }
}
