using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace xorWallet.Models;

public class Check
{
    [BsonId] public string Id { get; set; }

    public long CheckOwnerUid { get; set; }

    public int Xors { get; set; }
    public int Activations { get; set; }
    public long[] UserActivated { get; set; } = [];
}