using MongoDB.Driver;
using UniNotes.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace UniNotes.Services;
public class NoteService
{
    private readonly IMongoCollection<Note> _notes;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoDatabase _database;
    private readonly AuthenticationStateProvider _authStateProvider;

    public NoteService(IConfiguration config, AuthenticationStateProvider authStateProvider)
    {
        var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
        _database = mongoClient.GetDatabase("UniNotes");
        _notes = _database.GetCollection<Note>("Notes");
        _users = _database.GetCollection<User>("Users");
        _authStateProvider = authStateProvider;
    }

    public async Task<List<Note>> GetAllNotesAsync()
    {
        return await _notes.Find(_ => true).ToListAsync();
    }

    public async Task<List<Note>> GetNotesByUsernameAsync(string username)
    {
        return await _notes.Find(n => n.Username == username).ToListAsync();
    }

    public async Task<Note> GetNoteByIdAsync(string id)
    {
        return await _notes.Find(n => n.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> UploadNoteAsync(Note note, IBrowserFile file)
    {
        try
        {
            // Limit file size to 10 MB
            const long maxFileSize = 10 * 1024 * 1024;
            
            if (file.Size > maxFileSize)
                return false;

            using var stream = file.OpenReadStream(maxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            note.Content = memoryStream.ToArray();
            note.CreatedAt = DateTime.UtcNow;
            
            await _notes.InsertOneAsync(note);
            
            // Now associate the note's ID with the user
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

    public async Task<bool> DeleteNoteAsync(string id)
    {
        try
        {
            // First, find the note to get its username
            var note = await _notes.Find(n => n.Id == id).FirstOrDefaultAsync();
            if (note != null && !string.IsNullOrEmpty(note.Username))
            {
                // Remove the note ID from the associated user's noteIds array
                var updateDefinition = Builders<User>.Update.Pull(u => u.NoteIds, id);
                await _users.UpdateOneAsync(u => u.Username == note.Username, updateDefinition);
            }
            
            // Now delete the note
            var result = await _notes.DeleteOneAsync(n => n.Id == id);
            return result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RateNoteAsync(string noteId, int rating)
    {
        try
        {
            if (rating < 1 || rating > 5)
                return false;
            
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
            
            // Get the note
            var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
            if (note == null)
                return false;
            
            // Get the note author
            var noteAuthor = await _users.Find(u => u.Username == note.Username).FirstOrDefaultAsync();
            if (noteAuthor == null)
                return false;
            
            // Check if the user is trying to rate their own note
            if (noteAuthor.Id == userId)
                return false; // Prevent users from rating their own notes
            
            // Initialize ratings if null
            if (note.Ratings == null)
                note.Ratings = new List<Rating>();
            
            // Check if the user has already rated this note
            var existingRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
            
            // Track the rating change for author's average calculation
            int oldRatingValue = existingRating?.Value ?? 0;
            
            // Remove existing rating by this user if exists
            note.Ratings.RemoveAll(r => r.UserId == userId);
            
            // Add the new rating
            note.Ratings.Add(new Rating
            {
                UserId = userId,
                Username = currentUser.Username,
                Value = rating,
                CreatedAt = DateTime.UtcNow
            });
            
            // Recalculate average rating for the note
            note.AverageRating = note.Ratings.Any() 
                ? note.Ratings.Average(r => r.Value) 
                : 0;
            
            // Update the note in the database
            var noteFilter = Builders<Note>.Filter.Eq(n => n.Id, noteId);
            var noteUpdate = Builders<Note>.Update
                .Set(n => n.Ratings, note.Ratings)
                .Set(n => n.AverageRating, note.AverageRating);
            
            await _notes.UpdateOneAsync(noteFilter, noteUpdate);
            
            // Update the note author's average rating
            await UpdateUserAverageRatingAsync(noteAuthor.Username, oldRatingValue, rating);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int?> GetUserRatingAsync(string noteId, string userId)
    {
        var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
        if (note == null || note.Ratings == null)
            return null;
            
        var userRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
        return userRating?.Value;
    }

    public async Task<double> GetAverageRatingAsync(string noteId)
    {
        var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
        if (note == null || note.Ratings == null || !note.Ratings.Any())
            return 0;
        
        return note.Ratings.Average(r => r.Value);
    }

    public async Task<List<Rating>> GetRatingsAsync(string noteId)
    {
        var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
        return note?.Ratings ?? new List<Rating>();
    }

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
        
        // Get all users for efficient lookups
        var users = await _users.Find(_ => true).ToListAsync();
        var userLookup = users.ToDictionary(u => u.Username, u => u);
        
        foreach (var note in notes)
        {
            // Set the current user's rating
            if (note.Ratings != null)
            {
                var userRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
                note.UserRating = userRating?.Value;
            }
            
            // If we have the author in our lookup, add their average rating info
            // This could be useful in the UI to show author reputation
            if (!string.IsNullOrEmpty(note.Username) && userLookup.ContainsKey(note.Username))
            {
                var author = userLookup[note.Username];
                // You could add author information to the note if needed
                // For example, if you extend the Note class with AuthorAverageRating
            }
        }
        
        return notes;
    }

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
            
            // Get the note
            var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
            if (note == null)
                return false;
            
            // Get the note author
            var noteAuthor = await _users.Find(u => u.Username == note.Username).FirstOrDefaultAsync();
            if (noteAuthor == null)
                return false;
            
            // Initialize ratings if null
            if (note.Ratings == null)
                note.Ratings = new List<Rating>();
            
            // Check if user has a rating and get its value
            var existingRating = note.Ratings.FirstOrDefault(r => r.UserId == userId);
            if (existingRating == null)
                return false;
            
            int oldRatingValue = existingRating.Value;
            
            // Remove existing rating by this user
            note.Ratings.RemoveAll(r => r.UserId == userId);
            
            // Recalculate average rating
            note.AverageRating = note.Ratings.Any() 
                ? note.Ratings.Average(r => r.Value) 
                : 0;
            
            // Update the note in the database
            var filter = Builders<Note>.Filter.Eq(n => n.Id, noteId);
            var update = Builders<Note>.Update
                .Set(n => n.Ratings, note.Ratings)
                .Set(n => n.AverageRating, note.AverageRating);
            
            await _notes.UpdateOneAsync(filter, update);
            
            // Update the note author's average rating (removing the old rating)
            await UpdateUserAverageRatingAsync(noteAuthor.Username, oldRatingValue, 0);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task UpdateUserAverageRatingAsync(string username, int oldRatingValue, int newRatingValue)
    {
        try
        {
            var user = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
            if (user == null)
                return;
            
            // Get all notes by this user
            var userNotes = await _notes.Find(n => n.Username == username).ToListAsync();
            if (userNotes == null || !userNotes.Any())
                return;
            
            // Calculate the new total ratings received
            int totalRatings = userNotes.Sum(n => n.Ratings?.Count ?? 0);
            
            // Calculate the average rating across all notes
            double averageRating = 0;
            if (totalRatings > 0)
            {
                // Using direct calculation on all ratings from all notes
                averageRating = userNotes
                    .SelectMany(n => n.Ratings ?? new List<Rating>())
                    .Average(r => r.Value);
            }
            
            // Update user's average rating
            var userFilter = Builders<User>.Filter.Eq(u => u.Username, username);
            var userUpdate = Builders<User>.Update
                .Set(u => u.AverageNotesRating, averageRating)
                .Set(u => u.TotalRatingsReceived, totalRatings);
            
            await _users.UpdateOneAsync(userFilter, userUpdate);
        }
        catch
        {
            // Log error but continue execution
        }
    }

    public async Task<bool> RateNoteAsync(string noteId, string userId, string username, int rating)
    {
        try
        {
            if (rating < 1 || rating > 5)
                return false;
                
            // Get the note
            var note = await _notes.Find(n => n.Id == noteId).FirstOrDefaultAsync();
            if (note == null)
                return false;
            
            // Get the note author
            var noteAuthor = await _users.Find(u => u.Username == note.Username).FirstOrDefaultAsync();
            if (noteAuthor == null)
                return false;
            
            // Check if the user is trying to rate their own note
            if (noteAuthor.Id == userId)
                return false; // Prevent users from rating their own notes
            
            // Check for existing rating to track change
            var existingRating = note.Ratings?.FirstOrDefault(r => r.UserId == userId);
            int oldRatingValue = existingRating?.Value ?? 0;
            
            // Remove existing rating by this user if exists
            note.Ratings.RemoveAll(r => r.UserId == userId);
            
            // Add the new rating
            note.Ratings.Add(new Rating
            {
                UserId = userId,
                Username = username,
                Value = rating,
                CreatedAt = DateTime.UtcNow
            });
            
            // Recalculate average rating
            note.AverageRating = note.Ratings.Any() 
                ? note.Ratings.Average(r => r.Value) 
                : 0;
                
            // Update the note in the database
            var filter = Builders<Note>.Filter.Eq(n => n.Id, noteId);
            var update = Builders<Note>.Update
                .Set(n => n.Ratings, note.Ratings)
                .Set(n => n.AverageRating, note.AverageRating);
                
            await _notes.UpdateOneAsync(filter, update);
            
            // Update the note author's average rating
            await UpdateUserAverageRatingAsync(note.Username, oldRatingValue, rating);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Add this method for admin operations
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