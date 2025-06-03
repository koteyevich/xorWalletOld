using MongoDB.Driver;
using xorWallet.Models;

namespace xorWallet
{
    public class Database
    {
        //* the brain of the bot

        private readonly IMongoCollection<User> _userCollection;

        /// <summary>
        /// Constructor that connects to the MongoDB, gets the database, and inside that database gets a collection of users
        /// </summary>
        public Database()
        {
            //! remember to make a Secrets.cs file.
            //! also, remember to change the ip
            var connectionString = $"mongodb://{Secrets.DB_Username}:{Secrets.DB_Password}@192.168.222.222:27017";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("xorWallet");

            _userCollection = database.GetCollection<User>("Users");
        }

        /// <summary>
        /// Finds the user in a user collection by Telegram's userID
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed, this number is used in
        /// searching the user in a user collection</param>
        /// <returns>A <see cref="User"/> object. If no user is found, returns null.</returns>
        public async Task<User?> GetUserAsync(long userId)
        {
            return await _userCollection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a <see cref="User"/> in user collection.
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed, this number is used in
        /// creating the new user.</param>
        public async Task CreateUserAsync(long userId)
        {
            var user = new User { UserId = userId };
            await _userCollection.InsertOneAsync(user);
        }


        /// <summary>
        /// Increments the balance of a user with that userID and ensures that wallet does not go negative.
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed. This number is used to get the <see cref="User"/> from user collection, whose wallet we will increment by <paramref name="delta"/></param>
        /// <param name="delta">Is an increment by which we will modify how much money did we earn or spend.</param>
        /// <returns><c>false</c> if the balance is less than 0 (i.e., insufficient funds) or user is not found. <c>true</c> if everything went good </returns>
        public async Task<bool> UpdateBalanceAsync(long userId, int delta)
        {
            var user = await GetUserAsync(userId);

            if (user == null)
            {
                // User not found
                return false;
            }

            if (user.Balance + delta < 0)
            {
                return false;
            }

            var update = Builders<User>.Update.Inc(u => u.Balance, delta);
            var result = await _userCollection.UpdateOneAsync(u => u.UserId == userId, update);

            return result.ModifiedCount > 0;
        }
    }
}