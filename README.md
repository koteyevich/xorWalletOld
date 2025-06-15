# xorWallet

![banner.png](assets/banner.png 'image banner, it reads "xorWallet"')
> *"Exclusive or" is a logical operator whose negation is the logical
biconditional.* \- [Wikipedia](https://en.wikipedia.org/wiki/Exclusive_or)

xorWallet is a C# Telegram Bot Wallet that has its own currency **that has no value** - XOR.\
\
Bot offers checks, invoices, buying its currency, QR codes.

> [!CAUTION]
> XOR — bot's internal currency, which has **NO REAL VALUE**. Funding your account is a voluntary donation only.
**You will NOT be able to withdraw money or exchange XOR for real money.**

# Running own xorWallet

You want your own xorWallet? Follow these instructions to be set up.

## Bot

1. Clone the repository.
2. Open the project in the IDE of your preference.
3. Run `dotnet restore` (or IDE might do it for you)
4. In `src` folder, create `Secrets.cs` file.
    1. In there, create
       ```
       public enum Server
       {
           Test,
           Production
       }
       public static class Secrets
       {
           public const string PRODUCTION_TOKEN = "";
           public const string TEST_TOKEN = ""; 
           public const Server SERVER = Server.Production;

           public const string DB_USERNAME = "";
           public const string DB_PASSWORD = "";

           public const string CALLBACK_SALT = "";
       }
       ```
    2. Fill out these fields.

> [!WARNING]
> IMPORTANT: NEVER commit Secrets.cs to version control. <ins>Add it to .gitignore.</ins>
> The reason why this file is not present in this repository is because it was added to .gitignore, and so should you.

## Server (DB)

The database used is **MongoDB**. I recommend you to set up a docker container with a port `27017`, the default one.
> [!IMPORTANT]
> If you plan on changing the port, you need to change the port in Database.cs `connectionString`

After the container is created, you will need to create a database named `xorWallet`, inside it, you need to create a
collections named `Users`, `Checks`, `Invoices`.
> [!WARNING]
> Names are case-sensitive.

If you followed the instructions correctly, your bot and database should boot up, and everything should work correctly.
