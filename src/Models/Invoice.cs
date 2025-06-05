using MongoDB.Bson.Serialization.Attributes;

namespace xorWallet.Models
{
    public class Invoice
    {
        [BsonId] public string? Id { get; set; }

        public long InvoiceOwnerUid { get; set; }
        public int Xors { get; set; }
    }
}
