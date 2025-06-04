using MongoDB.Driver;
using xorWallet.Exceptions;
using xorWallet.Models;
using xorWallet.Processors;
using xorWallet.Utils;

namespace xorWallet
{
    public class Database
    {
        //* the brain of the bot

        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Check> _checkCollection;

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
            _checkCollection = database.GetCollection<Check>("Checks");
        }

        /// <summary>
        /// Finds the user in a user collection by Telegram's userID, if user does not exist, create one.
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed, this number is used in
        /// searching the user in a user collection</param>
        /// <returns>A <see cref="User"/> object.</returns>
        public async Task<User> GetUserAsync(long userId)
        {
            var user = await _userCollection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                await CreateUserAsync(userId);
                user = await _userCollection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
            }

            return user;
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
        /// <param name="delta">Is an increment by which we will modify how much money we earned or spend.</param>
        /// <exception cref="Exception">If something's with the user</exception>
        public async Task UpdateBalanceAsync(long userId, int delta)
        {
            var user = await GetUserAsync(userId);

            if (user.Balance + delta < 0)
            {
                throw new Message("User balance cannot be negative");
            }

            var update = Builders<User>.Update.Inc(u => u.Balance, delta);
            await _userCollection.UpdateOneAsync(u => u.UserId == userId, update);
        }

        /// <summary>
        /// <see cref="Check"/> is a way of sharing your xors to others using a link.
        /// In this method, we create one, but before so, we run a lot of checks for valid arguments and sufficient balance.
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed. This number is used to get the <see cref="User"/> from user collection,
        /// whose wallet we will increment by a negative value (aka decrease money) of xors * activation amount.</param>
        /// <param name="xors">How much xors does one activation gives.</param>
        /// <param name="activations">How many times can the check be activated?</param>
        /// <returns>The ID of the check that will be used in <see cref="CheckProcessor"/></returns>
        /// <exception cref="Message">If a value is invalid or if something's with the user.</exception>
        public async Task<string> CreateCheckAsync(long userId, int xors, int activations)
        {
            if (xors <= 0)
            {
                throw new Message("xors must be greater than or equal to zero");
            }

            if (activations <= 0)
            {
                throw new Message("activations must be greater than or equal to zero");
            }

            var user = await GetUserAsync(userId);

            if (user.Balance < xors * activations)
            {
                throw new Message("Insufficient balance");
            }

            // a lot of checks that can be messed up, I hope that everything will work...
            // and even worse is the database, that crap is fragile!

            var check = new Check()
            {
                Id = IdGenerator.GenerateId(),
                CheckOwnerUid = userId,
                Xors = xors,
                Activations = activations,
            };
            await UpdateBalanceAsync(userId, -(xors * activations));
            await _checkCollection.InsertOneAsync(check);

            return $"Check_{check.Id}";
        }

        /// <summary>
        /// Gets a <see cref="Check"/>, if it exists.
        /// </summary>
        /// <param name="checkId">Check ID that is used to find that check in a database.</param>
        /// <returns>А <see cref="Check"/>, if check is non-existent, null</returns>
        public async Task<Check?> GetCheckAsync(string checkId)
        {
            return await _checkCollection.Find(check => check.Id == checkId).FirstOrDefaultAsync();
        }

        public async Task UpdateCheckAsync(string checkId, long userId)
        {
            var check = await GetCheckAsync(checkId);
            if (check == null)
            {
                throw new Message("Check not found");
            }

            await UpdateCheckAsync(check, userId);
        }

        public async Task UpdateCheckAsync(Check check, long userId)
        {
            var update = Builders<Check>.Update
                .Inc(c => c.Activations, -1)
                .Push(c => c.UserActivated, userId);

            await _checkCollection.UpdateOneAsync(c => c.Id == check.Id, update);

            await UpdateBalanceAsync(userId, check.Xors);

            var updatedCheck = await GetCheckAsync(check.Id);
            if (updatedCheck is { Activations: <= 0 })
            {
                await _checkCollection.DeleteOneAsync(c => c.Id == updatedCheck.Id);
            }
        }
    }
}