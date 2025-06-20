using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;

namespace UniNotes.Models;

// Κλάση που αντιπροσωπεύει μια αξιολόγηση σημείωσης από έναν χρήστη
public class Rating
{
    // ID του χρήστη που έδωσε την αξιολόγηση
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    // Όνομα χρήστη του αξιολογητή
    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    // Η βαθμολογία που δόθηκε (από 1 έως 5 αστέρια)
    [BsonElement("value")]
    public int Value { get; set; } // 1-5 stars

    // Ημερομηνία και ώρα υποβολής της αξιολόγησης
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}