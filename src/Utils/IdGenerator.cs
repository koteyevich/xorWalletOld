using System.Text;

namespace xorWallet.Utils
{
    public abstract class IdGenerator
    {
        private static readonly char[] Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly Random Random = new();

        /// <summary>
        /// Generates a seasoned ID with time and random salt.
        /// </summary>
        public static string GenerateId()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            long salted = timestamp + Random.Next(0, 1 << 10);

            string base36Time = ToBase36(salted);
            string base36Rand = ToBase36(Random.Next(0, 36 * 36 * 36)).PadLeft(3, '0');

            return $"{base36Rand}{base36Time}";
        }

        private static string ToBase36(long value)
        {
            if (value == 0) return "0";

            var sb = new StringBuilder();
            while (value > 0)
            {
                sb.Insert(0, Base36Chars[value % 36]);
                value /= 36;
            }

            return sb.ToString();
        }
    }
}