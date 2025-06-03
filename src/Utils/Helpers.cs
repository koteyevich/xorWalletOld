namespace xorWallet.Utils
{
    public static class Helpers
    {
        //* the fingers of the bot


        private static readonly List<long> DeveloperIds = [935813811];

        public static async Task<bool> CheckDeveloper(long userId)
        {
            return await Task.FromResult(DeveloperIds.Contains(userId));
        }
    }
}