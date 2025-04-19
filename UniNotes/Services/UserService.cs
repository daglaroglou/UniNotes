using MongoDB.Driver;
using UniNotes.Models;
using System.Security.Cryptography;
using System.Text;

namespace UniNotes.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        // Property to store the currently logged-in user
        public User? CurrentUser { get; private set; }

        public UserService(IConfiguration config)
        {
            var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
            var database = mongoClient.GetDatabase("UniNotes");
            _users = database.GetCollection<User>("Users");
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            if (CurrentUser == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(CurrentUser.Id))
            {
                var refreshedUser = await _users.Find(x => x.Id == CurrentUser.Id).FirstOrDefaultAsync();
                if (refreshedUser != null)
                {
                    CurrentUser = refreshedUser;
                }
            }

            return CurrentUser;
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
            if (existingUserByEmail != null)
                return false;

            var existingUserByUsername = await GetByUsernameAsync(newUser.Username);
            if (existingUserByUsername != null)
                return false;

            newUser.PasswordHash = HashPassword(password);

            await _users.InsertOneAsync(newUser);
            return true;
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null)
                return false;

            if (VerifyPassword(password, user.PasswordHash))
            {
                await _users.ReplaceOneAsync(x => x.Id == user.Id, user);

                CurrentUser = user;
                return true;
            }

            return false;
        }

        public Task LogoutAsync()
        {
            CurrentUser = null;
            return Task.CompletedTask;
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
