using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniNotes.Models
{
    [BsonIgnoreExtraElements]
    [BsonDiscriminator("Notes")]
    public class Note
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;
        
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;
        
        [BsonElement("subject")]
        public string Subject { get; set; } = string.Empty;
        
        [BsonElement("semester")]
        public int Semester { get; set; } = 0;
        
        [BsonElement("content")]
        public Byte[] Content { get; set; } = [];
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;
        
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();
        
        [BsonElement("ratings")]
        public List<Rating> Ratings { get; set; } = new List<Rating>();
        
        [BsonElement("averageRating")]
        public double AverageRating { get; set; } = 0;

        [BsonIgnore]
        public int? UserRating { get; set; }
    }
}