namespace xorWallet.Utils
{
    public static class Helpers
    {
        //* the fingers of the bot


        private static readonly List<long> developer_ids = [935813811];

        public static async Task<bool> CheckDeveloper(long userId)
        {
            return await Task.FromResult(developer_ids.Contains(userId));
        }
    }
}
