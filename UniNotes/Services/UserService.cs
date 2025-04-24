using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using UniNotes.Models;

namespace UniNotes.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoDatabase _database;

        public UserService(IConfiguration config)
        {
            var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
            _database = mongoClient.GetDatabase("UniNotes");
            _users = _database.GetCollection<User>("Users");
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _users.Find(x => x.Id == userId).FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAsync() =>
            await _users.Find(_ => true).ToListAsync();

        public async Task<User?> GetByEmailAsync(string email) =>
            await _users.Find(x => x.Email == email).FirstOrDefaultAsync();

        public async Task<User?> GetByUsernameAsync(string username) =>
            await _users.Find(x => x.Username == username).FirstOrDefaultAsync();

        public async Task<bool> CreateAsync(User newUser, string password)
        {
            var existingUserByEmail = await GetByEmailAsync(newUser.Email);
            if (existingUserByEmail != null) return false;

            var existingUserByUsername = await GetByUsernameAsync(newUser.Username);
            if (existingUserByUsername != null) return false;

            newUser.PasswordHash = HashPassword(password);
            await _users.InsertOneAsync(newUser);
            return true;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return null;

            if (VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }

            return null;
        }

        public async Task<List<Note>> GetUserNotesAsync(string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null || user.NoteIds == null || !user.NoteIds.Any())
                return new List<Note>();

            // Get the notes collection
            var notesCollection = _database.GetCollection<Note>("Notes");
            
            // Build a filter to find notes with IDs in the user's noteIds list
            var filter = Builders<Note>.Filter.In(n => n.Id, user.NoteIds);
            
            // Return the notes
            return await notesCollection.Find(filter).ToListAsync();
        }

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

        private bool VerifyPassword(string password, string storedHash)
        {
            string passwordHash = HashPassword(password);
            return passwordHash == storedHash;
        }
    }
}