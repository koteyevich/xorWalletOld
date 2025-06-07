namespace xorWallet.Exceptions
{
    /// <summary>
    /// Error that shows to user, but is never reported to the developer.
    /// </summary>
    /// <param name="message">Error message</param>
    public class Message(string message) : Exception(message);
}
