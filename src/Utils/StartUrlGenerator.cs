namespace xorWallet.Utils
{
    public abstract class StartUrlGenerator
    {
        public static string GenerateStartUrl(string? data)
        {
            return $"https://t.me/xorwallet_bot?start={data}";
        }
    }
}
