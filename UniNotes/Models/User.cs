using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniNotes.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("uniIdNumber")]
        public string UniIdNumber { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isAdmin")]
        public bool IsAdmin { get; set; } = false;

        [BsonElement("isSecretary")]
        public bool IsSecretary { get; set; } = false;

        [BsonElement("notes")]
        public List<string> NoteIds { get; set; } = new List<string>();
        
        [BsonElement("averageNotesRating")]
        public double AverageNotesRating { get; set; } = 0;
        
        [BsonElement("totalRatingsReceived")]
        public int TotalRatingsReceived { get; set; } = 0;
    }
}
