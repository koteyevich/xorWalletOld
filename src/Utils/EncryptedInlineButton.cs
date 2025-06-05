using Telegram.Bot.Types.ReplyMarkups;

namespace xorWallet.Utils
{
    public static class EncryptedInlineButton
    {
        public static InlineKeyboardButton InlineButton(string text, string callback)
        {
            string encrypted = Encryption.EncryptCallback(callback);
            return new InlineKeyboardButton(text, encrypted);
        }
    }
}
