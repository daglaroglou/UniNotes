using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using UniNotes.Models;

namespace UniNotes.Services
{
    // Υπηρεσία διαχείρισης χρηστών - παρέχει λειτουργίες CRUD, αυθεντικοποίησης, και διαχείρισης κωδικών
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoDatabase _database;

        // Αρχικοποίηση της υπηρεσίας με σύνδεση στη MongoDB
        public UserService(IConfiguration config)
        {
            var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
            _database = mongoClient.GetDatabase("UniNotes");
            _users = _database.GetCollection<User>("Users");
        }

        // Ανάκτηση χρήστη με βάση το ID
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _users.Find(x => x.Id == userId).FirstOrDefaultAsync();
        }

        // Ανάκτηση όλων των χρηστών
        public async Task<List<User>> GetAsync() =>
            await _users.Find(_ => true).ToListAsync();

        // Ανάκτηση χρήστη με βάση το email
        public async Task<User?> GetByEmailAsync(string email) =>
            await _users.Find(x => x.Email == email).FirstOrDefaultAsync();

        // Ανάκτηση χρήστη με βάση το όνομα χρήστη
        public async Task<User?> GetByUsernameAsync(string username) =>
            await _users.Find(x => x.Username == username).FirstOrDefaultAsync();

        // Δεύτερη μέθοδος ανάκτησης χρήστη με βάση το όνομα χρήστη (για συμβατότητα)
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _users.Find(x => x.Username == username).FirstOrDefaultAsync();
        }

        // Δημιουργία νέου χρήστη με κωδικό πρόσβασης
        public async Task<bool> CreateAsync(User newUser, string password)
        {
            // Έλεγχος αν υπάρχει ήδη χρήστης με το ίδιο email
            var existingUserByEmail = await GetByEmailAsync(newUser.Email);
            if (existingUserByEmail != null) return false;

            // Έλεγχος αν υπάρχει ήδη χρήστης με το ίδιο όνομα χρήστη
            var existingUserByUsername = await GetByUsernameAsync(newUser.Username);
            if (existingUserByUsername != null) return false;

            // Κρυπτογράφηση του κωδικού και αποθήκευση του νέου χρήστη
            newUser.PasswordHash = HashPassword(password);
            await _users.InsertOneAsync(newUser);
            return true;
        }

        // Επικύρωση χρήστη με email και κωδικό πρόσβασης
        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return null;

            // Επαλήθευση του κωδικού πρόσβασης
            if (VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }

            return null;
        }

        // Ανάκτηση των σημειώσεων που έχει δημιουργήσει ένας χρήστης
        public async Task<List<Note>> GetUserNotesAsync(string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null || user.NoteIds == null || !user.NoteIds.Any())
                return new List<Note>();

            // Λήψη της συλλογής σημειώσεων
            var notesCollection = _database.GetCollection<Note>("Notes");

            // Δημιουργία φίλτρου για αναζήτηση σημειώσεων με IDs στη λίστα noteIds του χρήστη
            var filter = Builders<Note>.Filter.In(n => n.Id, user.NoteIds);

            // Επιστροφή των σημειώσεων
            return await notesCollection.Find(filter).ToListAsync();
        }

        // Ενημέρωση στοιχείων χρήστη
        public async Task<bool> UpdateUserAsync(User updatedUser)
        {
            if (updatedUser == null || string.IsNullOrEmpty(updatedUser.Id))
                return false;

            // Έλεγχος αν το όνομα χρήστη χρησιμοποιείται ήδη από άλλον χρήστη
            if (!string.IsNullOrEmpty(updatedUser.Username))
            {
                var existingUser = await GetByUsernameAsync(updatedUser.Username);
                if (existingUser != null && existingUser.Id != updatedUser.Id)
                    return false; // Το όνομα χρήστη χρησιμοποιείται από άλλον χρήστη
            }

            // Ενημέρωση των πεδίων που επιτρέπεται να αλλάξουν
            var update = Builders<User>.Update
                .Set(u => u.Username, updatedUser.Username)
                .Set(u => u.FirstName, updatedUser.FirstName)
                .Set(u => u.LastName, updatedUser.LastName)
                .Set(u => u.UniIdNumber, updatedUser.UniIdNumber);

            var result = await _users.UpdateOneAsync(u => u.Id == updatedUser.Id, update);
            return result.ModifiedCount > 0;
        }

        // Αλλαγή κωδικού πρόσβασης χρήστη
        public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword))
                return false;

            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // Κρυπτογράφηση του νέου κωδικού
            string newPasswordHash = HashPassword(newPassword);

            // Ενημέρωση του hash κωδικού
            var update = Builders<User>.Update
                .Set(u => u.PasswordHash, newPasswordHash);

            var result = await _users.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        // Διαγραφή λογαριασμού χρήστη και των σημειώσεών του
        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return false;

            try
            {
                // Λήψη της συλλογής σημειώσεων για καθαρισμό των σημειώσεων του χρήστη
                var notesCollection = _database.GetCollection<Note>("Notes");

                // Διαγραφή των σημειώσεων του χρήστη αν έχει κάποιες
                if (user.NoteIds != null && user.NoteIds.Any())
                {
                    var filter = Builders<Note>.Filter.In(n => n.Id, user.NoteIds);
                    await notesCollection.DeleteManyAsync(filter);
                }

                // Διαγραφή του λογαριασμού χρήστη
                var result = await _users.DeleteOneAsync(u => u.Id == userId);
                return result.DeletedCount > 0;
            }
            catch
            {
                // Καταγραφή του σφάλματος σε μια παραγωγική εφαρμογή
                return false;
            }
        }

        // Κρυπτογράφηση κωδικού πρόσβασης με SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashedBytes.Length; i++)
                {
                    builder.Append(hashedBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Επαλήθευση κωδικού πρόσβασης με το αποθηκευμένο hash
        private bool VerifyPassword(string password, string storedHash)
        {
            string passwordHash = HashPassword(password);
            return passwordHash == storedHash;
        }
    }
}