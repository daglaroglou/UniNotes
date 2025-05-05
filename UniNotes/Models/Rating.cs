using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;

namespace UniNotes.Models;

public class Rating
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;
    
    [BsonElement("value")]
    public int Value { get; set; } // 1-5 stars
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}