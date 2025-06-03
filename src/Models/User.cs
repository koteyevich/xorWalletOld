using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace xorWallet.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.Int64)]
    public long UserId { get; set; }

    [BsonElement("balance")] public int Balance { get; set; } = 0;
}