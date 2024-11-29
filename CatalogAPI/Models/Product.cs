using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models
{
    public enum Category
    {
        Electronics,
        Clothing,
        HomeAppliances,
        Books,
        Paintings,
        
        // tilføj eventuelt flere kategorier
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] // Ensures the Guid is stored as a string in MongoDB
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Category Category { get; set; } // Ændret fra string til enum
        public decimal Price { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
