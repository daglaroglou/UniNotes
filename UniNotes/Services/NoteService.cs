using MongoDB.Driver;
using UniNotes.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace UniNotes.Services;

// Υπηρεσία διαχείρισης σημειώσεων - παρέχει λειτουργίες CRUD, αξιολόγησης και αναζήτησης
public class NoteService
{
    private readonly IMongoCollection<Note> _notes;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoDatabase _database;
    private readonly AuthenticationStateProvider _authStateProvider;

    // Αρχικοποίηση της υπηρεσίας με σύνδεση στη MongoDB και τον AuthStateProvider
    public NoteService(IConfiguration config, AuthenticationStateProvider authStateProvider)
    {
        var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
        _database = mongoClient.GetDatabase("UniNotes");
        _notes = _database.GetCollection<Note>("Notes");
        _users = _database.GetCollection<User>("Users");
        _authStateProvider = authStateProvider;
    }

    // Ανάκτηση όλων των σημειώσεων από τη βάση δεδομένων
    public async Task<List<Note>> GetAllNotesAsync()
    {
        return await _notes.Find(_ => true).ToListAsync();
    }

    // Ανάκτηση σημειώσεων που ανήκουν σε συγκεκριμένο χρήστη με βάση το username
    public async Task<List<Note>> GetNotesByUsernameAsync(string username)
    {
        return await _notes.Find(n => n.Username == username).ToListAsync();
    }

    // Ανάκτηση συγκεκριμένης σημείωσης με βάση το ID της
    public async Task<Note> GetNoteByIdAsync(string id)
    {
        return await _notes.Find(n => n.Id == id).FirstOrDefaultAsync();
    }

    // Μεταφόρτωση νέας σημείωσης με αρχείο από τον χρήστη
    public async Task<bool> UploadNoteAsync(Note note, IBrowserFile file)
    {
        try
        {
            // Περιορισμός μεγέθους αρχείου στα 10 MB
            const long maxFileSize = 10 * 1024 * 1024;

            if (file.Size > maxFileSize)
                return false;

            // Διάβασμα του αρχείου σε μνήμη
            using var stream = file.OpenReadStream(maxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            // Αποθήκευση του περιεχομένου και της ημερομηνίας δημιουργίας
            note.Content = memoryStream.ToArray();
            note.CreatedAt = DateTime.UtcNow;

            // Εισαγωγή της σημείωσης στη βάση δεδομένων
            await _notes.InsertOneAsync(note);

            // Συσχέτιση του ID της σημείωσης με τον χρήστη
            if (!string.IsNullOrEmpty(note.Username) && !string.IsNullOrEmpty(note.Id))
            {
                var updateDefinition = Builders<User>.Update.Push(u => u.NoteIds, note.Id);
                await _users.UpdateOneAsync(u => u.Username == note.Username, updateDefinition);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Διαγραφή σημείωσης με βάση το ID της
    public async Task<bool> DeleteNoteAsync(string id)
    {
        try
        {
            // Πρώτα, εύρεση της σημείωσης για να πάρουμε το username
            var note = await _notes.Find(n => n.Id == id).FirstOrDefaultAsync();
            if (note != null && !string.IsNullOrEmpty(note.Username))
            {
                // Αφαίρεση του ID της σημείωσης από τον πίνακα noteIds του χρήστη
                var updateDefinition = Builders<User>.Update.Pull(u => u.NoteIds, id);
                await _users.UpdateOneAsync(u => u.Username == note.Username, updateDefinition);
            }

            // Διαγραφή της σημείωσης
            var result = await _notes.DeleteOneAsync(n => n.Id == id);
            return result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    // Βαθμολόγηση μιας σημείωσης από τον τρέχοντα συνδεδεμένο χρήστη
    public async Task<bool> RateNoteAsync(string noteId, int rating)
    {
        try
        {
            // Έλεγχος αν η βαθμολογία είναι έγκυρη (1-5)
            if (rating < 1 || rating > 5)
                return false;

            // Λήψη του τρέχοντος χρήστη από την κατάσταση αυθεντικοποίησης
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
                return false;

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return false;

            var currentUser = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (currentUser == null)
                return false;

            // Λήψη της σημείωσης
            var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
            if (note == null)
                return false;

            // Λήψη του συγγραφέα της σημείωσης
            var noteAuthor = await _users.Find(u => u.Username == note.Username).FirstOrDefaultAsync();
            if (noteAuthor == null)
                return false;

            // Έλεγχος αν ο χρήστης προσπαθεί να βαθμολογήσει τη δική του σημείωση
            if (noteAuthor.Id == userId)
                return false; // Αποτροπή βαθμολόγησης των δικών του σημειώσεων

            // Αρχικοποίηση των βαθμολογιών αν είναι null
            if (note.Ratings == null)
                note.Ratings = new List<Rating>();

            // Έλεγχος αν ο χρήστης έχει ήδη βαθμολογήσει αυτή τη σημείωση
            var existingRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);

            // Παρακολούθηση της αλλαγής βαθμολογίας για υπολογισμό μέσου όρου συγγραφέα
            int oldRatingValue = existingRating?.Value ?? 0;

            // Αφαίρεση υπάρχουσας βαθμολογίας από αυτόν τον χρήστη αν υπάρχει
            note.Ratings.RemoveAll(r => r.UserId == userId);

            // Προσθήκη νέας βαθμολογίας
            note.Ratings.Add(new Rating
            {
                UserId = userId,
                Username = currentUser.Username,
                Value = rating,
                CreatedAt = DateTime.UtcNow
            });

            // Επανυπολογισμός μέσου όρου βαθμολογίας για τη σημείωση
            note.AverageRating = note.Ratings.Any()
                ? note.Ratings.Average(r => r.Value)
                : 0;

            // Ενημέρωση της σημείωσης στη βάση δεδομένων
            var noteFilter = Builders<Note>.Filter.Eq(n => n.Id, noteId);
            var noteUpdate = Builders<Note>.Update
                .Set(n => n.Ratings, note.Ratings)
                .Set(n => n.AverageRating, note.AverageRating);

            await _notes.UpdateOneAsync(noteFilter, noteUpdate);

            // Ενημέρωση του μέσου όρου βαθμολογίας του συγγραφέα της σημείωσης
            await UpdateUserAverageRatingAsync(noteAuthor.Username, oldRatingValue, rating);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Ανάκτηση της βαθμολογίας που έχει δώσει ένας συγκεκριμένος χρήστης σε μια σημείωση
    public async Task<int?> GetUserRatingAsync(string noteId, string userId)
    {
        var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
        if (note == null || note.Ratings == null)
            return null;

        var userRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
        return userRating?.Value;
    }

    // Ανάκτηση του μέσου όρου βαθμολογίας μιας σημείωσης
    public async Task<double> GetAverageRatingAsync(string noteId)
    {
        var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
        if (note == null || note.Ratings == null || !note.Ratings.Any())
            return 0;

        return note.Ratings.Average(r => r.Value);
    }

    // Ανάκτηση όλων των βαθμολογιών μιας σημείωσης
    public async Task<List<Rating>> GetRatingsAsync(string noteId)
    {
        var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
        return note?.Ratings ?? new List<Rating>();
    }

    // Ανάκτηση όλων των σημειώσεων με τις βαθμολογίες του τρέχοντος χρήστη
    public async Task<List<Note>> GetNotesWithUserRatingsAsync()
    {
        var notes = await GetAllNotesAsync();

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity.IsAuthenticated)
            return notes;

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return notes;

        // Λήψη όλων των χρηστών για αποδοτικές αναζητήσεις
        var users = await _users.Find(_ => true).ToListAsync();
        var userLookup = users.ToDictionary(u => u.Username, u => u);

        foreach (var note in notes)
        {
            // Ορισμός της τρέχουσας βαθμολογίας του χρήστη
            if (note.Ratings != null)
            {
                var userRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
                note.UserRating = userRating?.Value;
            }

            // Αν έχουμε τον συγγραφέα στην αναζήτησή μας, προσθήκη πληροφοριών μέσου όρου
            // Αυτό θα μπορούσε να είναι χρήσιμο στο UI για εμφάνιση φήμης συγγραφέα
            if (!string.IsNullOrEmpty(note.Username) && userLookup.ContainsKey(note.Username))
            {
                var author = userLookup[note.Username];
                // Μπορείτε να προσθέσετε πληροφορίες συγγραφέα στη σημείωση αν χρειάζεται
                // Για παράδειγμα, αν επεκτείνετε την κλάση Note με AuthorAverageRating
            }
        }

        return notes;
    }

    // Αφαίρεση βαθμολογίας από μια σημείωση
    public async Task<bool> RemoveNoteRatingAsync(string noteId)
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
                return false;

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return false;

            // Λήψη της σημείωσης
            var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
            if (note == null)
                return false;

            // Λήψη του συγγραφέα της σημείωσης
            var noteAuthor = await _users.Find(u => u.Username == note.Username).FirstOrDefaultAsync();
            if (noteAuthor == null)
                return false;

            // Αρχικοποίηση βαθμολογιών αν είναι null
            if (note.Ratings == null)
                note.Ratings = new List<Rating>();

            // Έλεγχος αν ο χρήστης έχει βαθμολογία και λήψη της τιμής της
            var existingRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
            if (existingRating == null)
                return false;

            int oldRatingValue = existingRating.Value;

            // Αφαίρεση υπάρχουσας βαθμολογίας από αυτόν τον χρήστη
            note.Ratings.RemoveAll(r => r.UserId == userId);

            // Επανυπολογισμός μέσου όρου βαθμολογίας
            note.AverageRating = note.Ratings.Any()
                ? note.Ratings.Average(r => r.Value)
                : 0;

            // Ενημέρωση της σημείωσης στη βάση δεδομένων
            var filter = Builders<Note>.Filter.Eq(n => n.Id, noteId);
            var update = Builders<Note>.Update
                .Set(n => n.Ratings, note.Ratings)
                .Set(n => n.AverageRating, note.AverageRating);

            await _notes.UpdateOneAsync(filter, update);

            // Ενημέρωση του μέσου όρου βαθμολογίας του συγγραφέα (αφαιρώντας την παλιά βαθμολογία)
            await UpdateUserAverageRatingAsync(noteAuthor.Username, oldRatingValue, 0);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Ιδιωτική βοηθητική μέθοδος για ενημέρωση του μέσου όρου βαθμολογιών ενός χρήστη
    private async Task UpdateUserAverageRatingAsync(string username, int oldRatingValue, int newRatingValue)
    {
        try
        {
            var user = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
            if (user == null)
                return;

            // Λήψη όλων των σημειώσεων από αυτόν τον χρήστη
            var userNotes = await _notes.Find(n => n.Username == username).ToListAsync();
            if (userNotes == null || !userNotes.Any())
                return;

            // Υπολογισμός νέου συνόλου βαθμολογιών που έχει λάβει
            int totalRatings = userNotes.Sum(n => n.Ratings?.Count ?? 0);

            // Υπολογισμός μέσου όρου βαθμολογίας σε όλες τις σημειώσεις
            double averageRating = 0;
            if (totalRatings > 0)
            {
                // Χρήση άμεσου υπολογισμού σε όλες τις βαθμολογίες από όλες τις σημειώσεις
                averageRating = userNotes
                    .SelectMany(n => n.Ratings ?? new List<Rating>())
                    .Average(r => r.Value);
            }

            // Ενημέρωση του μέσου όρου βαθμολογίας του χρήστη
            var userFilter = Builders<User>.Filter.Eq(u => u.Username, username);
            var userUpdate = Builders<User>.Update
                .Set(u => u.AverageNotesRating, averageRating)
                .Set(u => u.TotalRatingsReceived, totalRatings);

            await _users.UpdateOneAsync(userFilter, userUpdate);
        }
        catch
        {
            // Καταγραφή σφάλματος αλλά συνέχιση εκτέλεσης
        }
    }

    // Βαθμολόγηση σημείωσης με καθορισμένα στοιχεία χρήστη (για χρήση από διαχειριστή)
    public async Task<bool> RateNoteAsync(string noteId, string userId, string username, int rating)
    {
        try
        {
            if (rating < 1 || rating > 5)
                return false;

            // Λήψη της σημείωσης
            var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
            if (note == null)
                return false;

            // Λήψη του συγγραφέα της σημείωσης
            var noteAuthor = await _users.Find(u => u.Username == note.Username).FirstOrDefaultAsync();
            if (noteAuthor == null)
                return false;

            // Έλεγχος αν ο χρήστης προσπαθεί να βαθμολογήσει τη δική του σημείωση
            if (noteAuthor.Id == userId)
                return false; // Αποτροπή βαθμολόγησης των δικών του σημειώσεων

            // Έλεγχος για υπάρχουσα βαθμολογία για παρακολούθηση αλλαγής
            var existingRating = note.Ratings?.FirstOrDefault(r => r.UserId == userId);
            int oldRatingValue = existingRating?.Value ?? 0;

            // Αφαίρεση υπάρχουσας βαθμολογίας από αυτόν τον χρήστη αν υπάρχει
            note.Ratings.RemoveAll(r => r.UserId == userId);

            // Προσθήκη νέας βαθμολογίας
            note.Ratings.Add(new Rating
            {
                UserId = userId,
                Username = username,
                Value = rating,
                CreatedAt = DateTime.UtcNow
            });

            // Επανυπολογισμός μέσου όρου βαθμολογίας
            note.AverageRating = note.Ratings.Any()
                ? note.Ratings.Average(r => r.Value)
                : 0;

            // Ενημέρωση της σημείωσης στη βάση δεδομένων
            var filter = Builders<Note>.Filter.Eq(n => n.Id, noteId);
            var update = Builders<Note>.Update
                .Set(n => n.Ratings, note.Ratings)
                .Set(n => n.AverageRating, note.AverageRating);

            await _notes.UpdateOneAsync(filter, update);

            // Ενημέρωση του μέσου όρου βαθμολογίας του συγγραφέα της σημείωσης
            await UpdateUserAverageRatingAsync(note.Username, oldRatingValue, rating);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Επανυπολογισμός όλων των μέσων όρων βαθμολογίας των χρηστών (για λειτουργίες διαχειριστή)
    public async Task<bool> RecalculateAllUsersAverageRatingsAsync()
    {
        try
        {
            var users = await _users.Find(_ => true).ToListAsync();

            foreach (var user in users)
            {
                var userNotes = await _notes.Find(n => n.Username == user.Username).ToListAsync();

                int totalRatings = userNotes.Sum(n => n.Ratings?.Count ?? 0);
                double averageRating = 0;

                if (totalRatings > 0)
                {
                    averageRating = userNotes
                        .SelectMany(n => n.Ratings ?? new List<Rating>())
                        .Average(r => r.Value);
                }

                var userFilter = Builders<User>.Filter.Eq(u => u.Username, user.Username);
                var userUpdate = Builders<User>.Update
                    .Set(u => u.AverageNotesRating, averageRating)
                    .Set(u => u.TotalRatingsReceived, totalRatings);

                await _users.UpdateOneAsync(userFilter, userUpdate);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}