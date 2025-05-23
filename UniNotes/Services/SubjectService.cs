using System.Text.Json;
using UniNotes.Models;

namespace UniNotes.Services
{
    public class SubjectService
    {
        private readonly IWebHostEnvironment _environment;
        private SubjectsData? _subjectsData;

        public SubjectService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<SubjectsData> GetSubjectsDataAsync()
        {
            if (_subjectsData != null)
            {
                return _subjectsData;
            }

            string filePath = Path.Combine(_environment.WebRootPath, "subjects.json");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Subjects file not found at {filePath}");
            }

            using FileStream stream = File.OpenRead(filePath);
            
            // Configure JSON options to be case-insensitive
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            _subjectsData = await JsonSerializer.DeserializeAsync<SubjectsData>(stream, options);
            
            return _subjectsData ?? new SubjectsData();
        }

        public async Task<List<string>> GetSubjectsForSemesterAsync(int semesterId)
        {
            var data = await GetSubjectsDataAsync();
            var semester = data.Semesters.FirstOrDefault(s => s.Id == semesterId);
            
            return semester?.Subjects ?? new List<string>();
        }
        
        public async Task<bool> SaveSubjectsDataAsync(SubjectsData subjectsData)
        {
            try
            {
                string filePath = Path.Combine(_environment.WebRootPath, "subjects.json");
                
                // Configure JSON options for pretty formatting
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                using FileStream stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, subjectsData, options);
                
                // Update the cached data
                _subjectsData = subjectsData;
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}