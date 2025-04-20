using MongoDB.Driver;
using UniNotes.Models;

namespace UniNotes.Services;
public class NoteService
{
    private readonly IMongoCollection<Note> _notesCollection;

    public NoteService(IMongoDatabase database)
    {
        _notesCollection = database.GetCollection<Note>("Notes");
    }

    public async Task<List<Note>> GetAllNotesAsync()
    {
        return await _notesCollection.Find(_ => true).ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(string id)
    {
        var filter = Builders<Note>.Filter.Eq(note => note.Id, id);
        return await _notesCollection.Find(filter).FirstOrDefaultAsync();
    }
    public async Task<Note> CreateNoteAsync(Note note)
    {
        await _notesCollection.InsertOneAsync(note);
        return note;
    }
    public async Task<bool> UpdateNoteAsync(string id, Note updatedNote)
    {
        var filter = Builders<Note>.Filter.Eq(note => note.Id, id);
        var result = await _notesCollection.ReplaceOneAsync(filter, updatedNote);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }
    public async Task<bool> DeleteNoteAsync(string id)
    {
        var filter = Builders<Note>.Filter.Eq(note => note.Id, id);
        var result = await _notesCollection.DeleteOneAsync(filter);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
    public async Task<List<Note>> GetNotesByUsernameAsync(string userId)
    {
        var filter = Builders<Note>.Filter.Eq(note => note.Username, userId);
        return await _notesCollection.Find(filter).ToListAsync();
    }
    public async Task<List<Note>> GetNotesBySubjectAsync(string subject)
    {
        var filter = Builders<Note>.Filter.Eq(note => note.Subject, subject);
        return await _notesCollection.Find(filter).ToListAsync();
    }
    public async Task<List<Note>> GetNotesBySemesterAsync(int semester)
    {
        var filter = Builders<Note>.Filter.Eq(note => note.Semester, semester);
        return await _notesCollection.Find(filter).ToListAsync();
    }
}