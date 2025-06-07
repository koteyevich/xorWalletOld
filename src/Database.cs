using System.Text;
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

        private readonly IMongoCollection<User> userCollection;
        private readonly IMongoCollection<Check> checkCollection;
        private readonly IMongoCollection<Invoice> invoiceCollection;

        /// <summary>
        /// Constructor that connects to the MongoDB, gets the database, and inside that database gets a collection of users
        /// </summary>
        public Database()
        {
            //! remember to make a Secrets.cs file.
            //! also, remember to change the ip
            string connectionString = $"mongodb://{Secrets.DB_USERNAME}:{Secrets.DB_PASSWORD}@192.168.222.222:27017";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("xorWallet");

            userCollection = database.GetCollection<User>("Users");
            checkCollection = database.GetCollection<Check>("Checks");
            invoiceCollection = database.GetCollection<Invoice>("Invoices");
        }

        /// <summary>
        /// Finds the user in a user collection by Telegram's userID, if user does not exist, create one.
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed, this number is used in
        /// searching the user in a user collection</param>
        /// <returns>A <see cref="User"/> object.</returns>
        public async Task<User> GetUserAsync(long userId)
        {
            var user = await userCollection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                await createUserAsync(userId);
                user = await userCollection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
            }

            return user;
        }

        /// <summary>
        /// Creates a <see cref="User"/> in user collection.
        /// </summary>
        /// <param name="userId">Telegram userID is unique for each user and can't be changed, this number is used in
        /// creating the new user.</param>
        private async Task createUserAsync(long userId)
        {
            var user = new User { UserId = userId };
            await userCollection.InsertOneAsync(user);
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
                throw new Message("User balance cannot be negative. Insufficient balance.");
            }

            var update = Builders<User>.Update.Inc(u => u.Balance, delta);
            await userCollection.UpdateOneAsync(u => u.UserId == userId, update);
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
            await checkCollection.InsertOneAsync(check);

            return $"Check_{check.Id}";
        }

        /// <summary>
        /// Gets a <see cref="Check"/>, if it exists.
        /// </summary>
        /// <param name="checkId">Check ID used to find that check in a database.</param>
        /// <returns>А <see cref="Check"/>, if check is non-existent, null</returns>
        public async Task<Check?> GetCheckAsync(string? checkId)
        {
            return await checkCollection.Find(check => check.Id == checkId).FirstOrDefaultAsync();
        }

        public async Task UpdateCheckAsync(string? checkId, long userId)
        {
            var check = await GetCheckAsync(checkId);
            if (check == null)
            {
                throw new Message("Check not found");
            }

            await UpdateCheckAsync(check, userId);
        }

        /// <summary>
        /// Increments <see cref="Check"/> by -1, adds user to an array of people who activated the check,
        /// and add XORs to the balance of that user
        /// </summary>
        /// <param name="check"><see cref="Check"/> object.</param>
        /// <param name="userId">Telegram UserID is unique for each user and can't be changed. This number is used to add the user to the list of
        /// users that have activated the check so they can't activate it again. Then increment the balance of that user by amount of XORs the check gives each activation.</param>
        public async Task UpdateCheckAsync(Check check, long userId)
        {
            var update = Builders<Check>.Update
                .Inc(c => c.Activations, -1)
                .Push(c => c.UserActivated, userId);

            await checkCollection.UpdateOneAsync(c => c.Id == check.Id, update);

            await UpdateBalanceAsync(userId, check.Xors);

            var updatedCheck = await GetCheckAsync(check.Id);
            if (updatedCheck is { Activations: <= 0 })
            {
                await RemoveCheckAsync(updatedCheck.Id);
            }
        }

        /// <summary>
        /// Deletes the check.
        /// </summary>
        /// <param name="checkId">Check ID used to delete the check.</param>
        public async Task RemoveCheckAsync(string? checkId)
        {
            await checkCollection.DeleteOneAsync(c => c.Id == checkId);
        }

        /// <summary>
        /// Invoice is a way to charge someone or pay for something. In this method, we create one.
        /// </summary>
        /// <param name="userId">Telegram UserID is a unique number that can never be changed. Here, we use it to store the owner of the check</param>
        /// <param name="xors">The amount of XORs user has to pay (not owner)</param>
        /// <returns>A string that can be used as /start argument. </returns>
        /// <exception cref="Message">Used as ArgumentException.</exception>
        public async Task<string> CreateInvoiceAsync(long userId, int xors)
        {
            if (xors <= 0)
            {
                throw new Message("xors must be greater than or equal to zero");
            }

            var user = await GetUserAsync(userId);

            var invoice = new Invoice()
            {
                Id = IdGenerator.GenerateId(),
                InvoiceOwnerUid = user.UserId,
                Xors = xors,
            };

            await invoiceCollection.InsertOneAsync(invoice);
            return $"Invoice_{invoice.Id}";
        }

        /// <summary>
        /// Gets <see cref="Invoice"/> object by Invoice ID
        /// </summary>
        /// <param name="invoiceId">ID that is used to get <see cref="Invoice"/> object</param>
        /// <returns><see cref="Invoice"/> object.</returns>
        public async Task<Invoice?> GetInvoiceAsync(string? invoiceId)
        {
            return await invoiceCollection.Find(invoice => invoice.Id == invoiceId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Delete invoice by invoice ID
        /// </summary>
        /// <param name="invoiceId">ID of <see cref="Invoice"/> object.</param>
        public async Task RemoveInvoiceAsync(string? invoiceId)
        {
            await invoiceCollection.DeleteOneAsync(c => c.Id == invoiceId);
        }

        /// <summary>
        /// User might want to get the list of his active checks and possibly delete one.
        /// </summary>
        /// <param name="userId">Telegrams UserID that is used to search in a check collection for the checks user owns and are currently active.</param>
        /// <returns><see cref="StringBuilder"/> that then can be converted to <c>string</c> later.</returns>
        public async Task<StringBuilder> ListUserChecks(long userId)
        {
            var cursor = await checkCollection.FindAsync(c => c.CheckOwnerUid == userId);
            var checks = await cursor.ToListAsync();

            var sb = new StringBuilder();
            if (checks.Count == 0)
            {
                sb.AppendLine("У вас нет активных чеков.");
            }
            else
            {
                sb.AppendLine("Ваши активные чеки:");
                foreach (var check in checks)
                {
                    sb.AppendLine(
                        $"• ID: <code>{check.Id}</code> — {check.Xors} XOR, осталось активаций: {check.Activations} <a href=\"{StartUrlGenerator.GenerateStartUrl($"Check_{check.Id}")}\">🗑</a>");
                }
            }

            return sb;
        }

        /// <summary>
        /// User might want to get the list of his active invoices and possibly delete one.
        /// </summary>
        /// <param name="userId">Telegrams UserID that is used to search in a invoice collection for the invoices user owns and are currently active.</param>
        /// <returns><see cref="StringBuilder"/> that then can be converted to <c>string</c> later.</returns>
        public async Task<StringBuilder> ListUserInvoices(long userId)
        {
            var cursor = await invoiceCollection.FindAsync(c => c.InvoiceOwnerUid == userId);
            var invoices = await cursor.ToListAsync();

            var sb = new StringBuilder();
            if (invoices.Count == 0)
            {
                sb.AppendLine("У вас нет активных счетов.");
            }
            else
            {
                sb.AppendLine("Ваши активные счета:");
                foreach (var invoice in invoices)
                {
                    sb.AppendLine(
                        $"• ID: <code>{invoice.Id}</code> — {invoice.Xors} XOR <a href=\"{StartUrlGenerator.GenerateStartUrl($"Invoice_{invoice.Id}")}\">🗑</a>");
                }
            }

            return sb;
        }
    }
}
