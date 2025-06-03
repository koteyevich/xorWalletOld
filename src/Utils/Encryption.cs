using System.Security.Cryptography;
using System.Text;

namespace xorWallet.Utils
{
    public static class Encryption
    {
        private static readonly byte[] AesKey = SHA256.HashData(Encoding.UTF8.GetBytes(Secrets.CallbackSalt));
        private static readonly byte[] AesIv = AesKey.Take(16).ToArray();

        public static string EncryptCallback(string data)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = AesKey;
                aes.IV = AesIv;

                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data + Secrets.CallbackSalt));
                var shortHash = hash.Take(4).ToArray();

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var combined = new byte[dataBytes.Length + shortHash.Length];
                dataBytes.CopyTo(combined, 0);
                shortHash.CopyTo(combined, dataBytes.Length);

                using var encryptor = aes.CreateEncryptor();
                var encrypted = encryptor.TransformFinalBlock(combined, 0, combined.Length);

                var base64 = Convert.ToBase64String(encrypted)
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
                var base64 = encrypted.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var encryptedBytes = Convert.FromBase64String(base64);

                using var aes = Aes.Create();
                aes.Key = AesKey;
                aes.IV = AesIv;

                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                var dataLength = decrypted.Length - 4;
                var data = Encoding.UTF8.GetString(decrypted, 0, dataLength);
                var receivedHash = decrypted.Skip(dataLength).Take(4).ToArray();

                var computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(data + Secrets.CallbackSalt));
                var shortHash = computedHash.Take(4).ToArray();

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