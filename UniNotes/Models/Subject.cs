using System.Text.Json.Serialization;

namespace UniNotes.Models
{
    public class SemesterSubjects
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("subjects")]
        public List<string> Subjects { get; set; } = new List<string>();
    }

    public class SubjectsData
    {
        [JsonPropertyName("semesters")]
        public List<SemesterSubjects> Semesters { get; set; } = new List<SemesterSubjects>();
    }
}