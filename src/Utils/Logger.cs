using System.Collections.Concurrent;
using System.Text.Json;

namespace xorWallet.Utils
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Command = 2,
        Warn = 3,
        Error = 4,
        None = 5
    }

    public class LogTag(string name, ConsoleColor color)
    {
        public string Name { get; set; } = name;
        public ConsoleColor Color { get; set; } = color;
    }

    public static class Logger
    {
        private static LogLevel currentLevel = LogLevel.Debug;
        private static readonly string log_directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string log_file_prefix = "xorWallet_";

        private static readonly
            ConcurrentQueue<(string Message, string[] Tags, LogLevel Level, DateTime Timestamp,
                Dictionary<string, object>? Context)> log_queue = new();

        private static readonly CancellationTokenSource cts = new();
        private static readonly Task logging_task;
        private static int maxLogFileSizeMb = 10; // Configurable max file size in MB

        private static readonly Dictionary<string, ConsoleColor> tag_colors = new()
        {
            { "DEBUG", ConsoleColor.Gray },
            { "INFO", ConsoleColor.Cyan },
            { "WARN", ConsoleColor.Yellow },
            { "ERROR", ConsoleColor.Red },
            { "SUCCESS", ConsoleColor.Green },
            { "COMMAND", ConsoleColor.Magenta },
            { "MESSAGE", ConsoleColor.White },
            {
                "DATABASE", ConsoleColor.DarkYellow
            },
            { "BOT", ConsoleColor.DarkCyan },
        };

        static Logger()
        {
            Directory.CreateDirectory(log_directory);
            logging_task = Task.Run(processLogQueue, cts.Token);
            AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdown();
        }

        public static LogLevel CurrentLevel
        {
            get => currentLevel;
            set
            {
                currentLevel = value;
                info($"Log level set to: {value}");
            }
        }

        private static bool logToFile { get; set; } = true;

        public static int MaxLogFileSizeMb
        {
            get => maxLogFileSizeMb;
            set => maxLogFileSizeMb = value > 0 ? value : 1;
        }

        private static void enqueueLog(string message, string[] tags, LogLevel level,
            Dictionary<string, object>? context = null)
        {
            if (level < currentLevel || (tags.Contains("DEBUG", StringComparer.OrdinalIgnoreCase) &&
                                         currentLevel > LogLevel.Debug))
                return;

            log_queue.Enqueue((message, tags, level, DateTime.UtcNow, context));
        }

        private static async Task processLogQueue()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                while (log_queue.TryDequeue(out var logEntry))
                {
                    await writeLog(logEntry.Message, logEntry.Tags, logEntry.Level, logEntry.Timestamp,
                        logEntry.Context);
                }

                await Task.Delay(100); // Throttle to prevent tight loop
            }
        }

        private static async Task writeLog(string message, string[] tags, LogLevel level, DateTime timestamp,
            Dictionary<string, object>? context)
        {
            // Console output
            Console.ResetColor();
            Console.Write($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            foreach (string tag in tags)
            {
                var color = tag_colors.GetValueOrDefault(tag.ToUpper(), ConsoleColor.White);
                Console.ForegroundColor = color;
                Console.Write($"[{tag}]");
                Console.ResetColor();
            }

            Console.Write(" ");
            Console.WriteLine(message);

            // File output
            if (logToFile)
            {
                string logFile = getCurrentLogFile();
                checkLogFileSize(logFile);
                var logEntry = new
                {
                    Timestamp = timestamp.ToString("o"),
                    Level = level.ToString(),
                    Tags = tags,
                    Message = message,
                    Context = context
                };
                string jsonLog = JsonSerializer.Serialize(logEntry);
                await File.AppendAllTextAsync(logFile, jsonLog + Environment.NewLine);
            }
        }

        private static string getCurrentLogFile()
        {
            return Path.Combine(log_directory, $"{log_file_prefix}{DateTime.UtcNow:yyyy-MM-dd}.log");
        }

        private static void checkLogFileSize(string logFile)
        {
            if (!File.Exists(logFile)) return;
            var fileInfo = new FileInfo(logFile);
            if (fileInfo.Length > maxLogFileSizeMb * 1024 * 1024)
            {
                string archiveFile = Path.Combine(log_directory,
                    $"{log_file_prefix}{DateTime.UtcNow:yyyy-MM-dd_HHmmss}.log");
                File.Move(logFile, archiveFile);
            }
        }

        private static void shutdown()
        {
            cts.Cancel();
            logging_task.Wait(); // Ensure all logs are written
        }

        public static void Log(string message, params string[] tags) => enqueueLog(message, tags, LogLevel.Info);

        public static void Debug(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("DEBUG").ToArray(), LogLevel.Debug);

        private static void info(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("INFO").ToArray(), LogLevel.Info);

        public static void Warn(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("WARN").ToArray(), LogLevel.Warn);

        public static void Error(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("ERROR").ToArray(), LogLevel.Error);

        public static void Success(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("SUCCESS").ToArray(), LogLevel.Info);

        public static void Command(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("COMMAND").ToArray(), LogLevel.Command);

        public static void Admin(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("ADMIN").ToArray(), LogLevel.Command);

        public static void Message(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("MESSAGE").ToArray(), LogLevel.Debug);

        public static void Database(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("DATABASE").ToArray(), LogLevel.Debug);

        public static void Bot(string message, params string[] tags) =>
            enqueueLog(message, tags.Prepend("BOT").ToArray(), LogLevel.Info);

        // New method to include context
        public static void LogWithContext(string message, Dictionary<string, object> context, params string[] tags)
        {
            enqueueLog(message, tags, LogLevel.Info, context);
        }

        public static void SetLogLevel(string level)
        {
            if (Enum.TryParse(level, true, out LogLevel parsedLevel))
            {
                CurrentLevel = parsedLevel;
            }
            else
            {
                Error($"Invalid log level: {level}");
            }
        }
    }
}
