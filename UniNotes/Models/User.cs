using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniNotes.Models
{
    // Κλάση που αντιπροσωπεύει έναν χρήστη της εφαρμογής
    public class User
    {
        // Μοναδικό αναγνωριστικό του χρήστη στη MongoDB
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Μικρό όνομα του χρήστη
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        // Επώνυμο του χρήστη
        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        // Όνομα χρήστη (username) για σύνδεση
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        // Διεύθυνση email του χρήστη (πρέπει να είναι του πανεπιστημίου)
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        // Κρυπτογραφημένος κωδικός πρόσβασης του χρήστη
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        // Αριθμός φοιτητικής ταυτότητας του χρήστη
        [BsonElement("uniIdNumber")]
        public string UniIdNumber { get; set; } = string.Empty;

        // Ημερομηνία και ώρα δημιουργίας του λογαριασμού
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Ένδειξη αν ο χρήστης είναι διαχειριστής του συστήματος
        [BsonElement("isAdmin")]
        public bool IsAdmin { get; set; } = false;

        // Ένδειξη αν ο χρήστης έχει δικαιώματα γραμματείας
        [BsonElement("isSecretary")]
        public bool IsSecretary { get; set; } = false;

        // Λίστα με τα IDs των σημειώσεων που έχει αναρτήσει ο χρήστης
        [BsonElement("notes")]
        public List<string> NoteIds { get; set; } = new List<string>();

        // Μέσος όρος αξιολογήσεων των σημειώσεων του χρήστη
        [BsonElement("averageNotesRating")]
        public double AverageNotesRating { get; set; } = 0;

        // Συνολικός αριθμός αξιολογήσεων που έχουν λάβει οι σημειώσεις του χρήστη
        [BsonElement("totalRatingsReceived")]
        public int TotalRatingsReceived { get; set; } = 0;
    }
}