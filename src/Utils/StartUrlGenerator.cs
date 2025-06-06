namespace xorWallet.Utils
{
    public class StartUrlGenerator
    {
        public static string GenerateStartUrl(string data)
        {
            return $"https://t.me/xorwallet_bot?start={data}";
        }
    }
}
