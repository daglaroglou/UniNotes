using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniNotes.Models
{
    // Κλάση που αντιπροσωπεύει μια σημείωση/έγγραφο στο σύστημα
    // Χρησιμοποιείται για αποθήκευση και προβολή σημειώσεων μαθημάτων
    [BsonIgnoreExtraElements]
    [BsonDiscriminator("Notes")]
    public class Note
    {
        // Μοναδικό αναγνωριστικό της σημείωσης στη MongoDB
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Τίτλος της σημείωσης
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        // Περιγραφή της σημείωσης
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        // Μάθημα στο οποίο αναφέρεται η σημείωση
        [BsonElement("subject")]
        public string Subject { get; set; } = string.Empty;

        // Εξάμηνο στο οποίο διδάσκεται το μάθημα (1-8)
        [BsonElement("semester")]
        public int Semester { get; set; } = 0;

        // Δυαδικά περιεχόμενα του αρχείου σημείωσης (PDF, εικόνα, Word, κλπ.)
        [BsonElement("content")]
        public Byte[] Content { get; set; } = [];

        // Ημερομηνία και ώρα δημιουργίας της σημείωσης
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Όνομα χρήστη του δημιουργού της σημείωσης
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        // Ετικέτες που περιγράφουν το περιεχόμενο της σημείωσης (π.χ. "Εξετάσεις", "Διάλεξη")
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        // Λίστα αξιολογήσεων που έχει λάβει η σημείωση από άλλους χρήστες
        [BsonElement("ratings")]
        public List<Rating> Ratings { get; set; } = new List<Rating>();

        // Μέσος όρος αξιολόγησης από όλες τις βαθμολογίες
        [BsonElement("averageRating")]
        public double AverageRating { get; set; } = 0;

        // Η αξιολόγηση του τρέχοντος χρήστη (εάν υπάρχει)
        // Δεν αποθηκεύεται στη βάση, χρησιμοποιείται μόνο για την προβολή
        [BsonIgnore]
        public int? UserRating { get; set; }
    }
}