using System.Security.Cryptography;
using System.Text;

namespace xorWallet.Utils
{
    public static class Encryption
    {
        private static readonly byte[] aes_key = SHA256.HashData(Encoding.UTF8.GetBytes(Secrets.CALLBACK_SALT));
        private static readonly byte[] aes_iv = aes_key.Take(16).ToArray();

        public static string EncryptCallback(string data)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = aes_key;
                aes.IV = aes_iv;

                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data + Secrets.CALLBACK_SALT));
                byte[] shortHash = hash.Take(4).ToArray();

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] combined = new byte[dataBytes.Length + shortHash.Length];
                dataBytes.CopyTo(combined, 0);
                shortHash.CopyTo(combined, dataBytes.Length);

                using var encryptor = aes.CreateEncryptor();
                byte[] encrypted = encryptor.TransformFinalBlock(combined, 0, combined.Length);

                string base64 = Convert.ToBase64String(encrypted)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');

                // Validate length (Telegram callback data limit: 64 bytes)
                if (base64.Length > 64)
                {
                    Logger.Error($"Encrypted callback exceeds 64 bytes: {base64.Length}, Data: {data}", "ENCRYPTION");
                    throw new Exception($"Encrypted callback too long: {base64.Length} bytes");
                }

                return base64;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error encrypting callback: {ex.Message}, Data: {data}", "ENCRYPTION");
                throw;
            }
        }

        public static string? DecryptCallback(string encrypted)
        {
            try
            {
                string base64 = encrypted.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                byte[] encryptedBytes = Convert.FromBase64String(base64);

                using var aes = Aes.Create();
                aes.Key = aes_key;
                aes.IV = aes_iv;

                using var decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                int dataLength = decrypted.Length - 4;
                string data = Encoding.UTF8.GetString(decrypted, 0, dataLength);
                byte[] receivedHash = decrypted.Skip(dataLength).Take(4).ToArray();

                byte[] computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(data + Secrets.CALLBACK_SALT));
                byte[] shortHash = computedHash.Take(4).ToArray();

                if (!shortHash.SequenceEqual(receivedHash))
                {
                    Logger.Error($"Callback hash verification failed, Encrypted: {encrypted}, Decrypted: {data}",
                        "ENCRYPTION");
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error decrypting callback: {ex.Message}, Encrypted: {encrypted}", "ENCRYPTION");
                return null;
            }
        }
    }
}
