using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Utils;

namespace xorWallet.Commands
{
    public class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> commands = new();

        public CommandRegistry()
        {
            var commandList = new List<ICommand>
            {
                new Start(),
                new Balance(),
                new Check(),
                new Invoice(),
                new MyChecks(),
                new MyInvoices(),
                // Add more commands here
            };

            foreach (var command in commandList)
            {
                commands[command.Name.ToLower()] = command;

                foreach (string alias in command.Aliases)
                {
                    commands[alias.ToLower()] = command;
                }
            }
        }


        public async Task HandleCommandAsync(Message message, TelegramBotClient bot)
        {
            if (string.IsNullOrEmpty(message.Text))
                return;

            string commandText = message.Text.Split(' ')[0].ToLower();

            string normalizedCommand = commandText.Split('@')[0];

            var matchingCommand = commands
                .FirstOrDefault(kvp => normalizedCommand.StartsWith(kvp.Key));

            if (matchingCommand.Value != null)
            {
                if (matchingCommand.Value is IDebugCommand)
                {
                    Logger.Command($"Processing debug command: {normalizedCommand}", "DEBUG");
                }

                await matchingCommand.Value.ExecuteAsync(message, bot);
            }
            else if (normalizedCommand.StartsWith("/help"))
            {
                await SendHelpMessage(message, bot);
            }
        }

        public async Task SendHelpMessage(Message message, TelegramBotClient bot)
        {
            string helpMessage = "<b>Доступные команды:</b>\n\n";
            var listed = new HashSet<ICommand>();

            foreach (var command in commands.Values.Distinct())
            {
                if (!listed.Add(command)) continue;

                if (command is IDebugCommand debugCommand && debugCommand.RequiresDeveloper)
                {
                    if (message.From != null && !await Helpers.CheckDeveloper(message.From.Id))
                        continue;
                }

                string aliasText = command.Aliases.Length > 0
                    ? $" ({string.Join(", ", command.Aliases)})"
                    : "";

                helpMessage += $"{command.Name}{aliasText} - {command.Description}\n";
            }

            await bot.SendMessage(message.Chat.Id, helpMessage, Telegram.Bot.Types.Enums.ParseMode.Html,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });
        }

        public IReadOnlyDictionary<string, ICommand> Commands => commands.AsReadOnly();
    }
}
