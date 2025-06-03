namespace xorWallet.Exceptions
{
    public class BotException : Exception
    {
        public BotException(string message) : base(message) { }
        public BotException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}