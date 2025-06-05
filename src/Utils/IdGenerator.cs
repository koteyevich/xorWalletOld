using System.Text;

namespace xorWallet.Utils
{
    public abstract class IdGenerator
    {
        private static readonly char[] base36_chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly Random random = new();

        /// <summary>
        /// Generates a seasoned ID with time and random salt.
        /// </summary>
        public static string GenerateId()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            long salted = timestamp + random.Next(0, 1 << 10);

            string base36Time = toBase36(salted);
            string base36Rand = toBase36(random.Next(0, 36 * 36 * 36)).PadLeft(3, '0');

            return $"{base36Rand}{base36Time}";
        }

        private static string toBase36(long value)
        {
            if (value == 0) return "0";

            var sb = new StringBuilder();
            while (value > 0)
            {
                sb.Insert(0, base36_chars[value % 36]);
                value /= 36;
            }

            return sb.ToString();
        }
    }
}
