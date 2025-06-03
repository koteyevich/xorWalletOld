using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Utils;

namespace xorWallet.Interfaces
{
    public interface IDebugCommand : ICommand
    {
        bool RequiresDeveloper { get; }
    }

    public abstract class DebugCommandBase : IDebugCommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public string[] Aliases => [];

        public virtual bool RequiresAdmin => true;
        public virtual bool RequiresDeveloper => true;

        public async Task ExecuteAsync(Message message, TelegramBotClient? bot)
        {
            if (RequiresDeveloper && message.From != null && !await Helpers.CheckDeveloper(message.From.Id))
            {
                if (bot != null) await ExecuteCoreAsync(message, bot);
            }
        }

        protected abstract Task ExecuteCoreAsync(Message message, TelegramBotClient bot);
    }
}