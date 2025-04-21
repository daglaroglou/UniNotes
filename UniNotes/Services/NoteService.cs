using MongoDB.Driver;
using UniNotes.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;

namespace UniNotes.Services;
public class NoteService
{
    private readonly IMongoCollection<Note> _notes;
    private readonly IMongoDatabase _database;

    public NoteService(IConfiguration config)
    {
        var mongoClient = new MongoClient(config.GetConnectionString("MongoDb"));
        _database = mongoClient.GetDatabase("UniNotes");
        _notes = _database.GetCollection<Note>("Notes");
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
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteNoteAsync(string id)
    {
        var result = await _notes.DeleteOneAsync(n => n.Id == id);
        return result.DeletedCount > 0;
    }
}