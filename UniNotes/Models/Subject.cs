using System.Text.Json.Serialization;

namespace UniNotes.Models
{
    // Κλάση που περιέχει τα μαθήματα ενός συγκεκριμένου εξαμήνου
    public class SemesterSubjects
    {
        // Αριθμός εξαμήνου (από 1 έως 8)
        [JsonPropertyName("id")]
        public int Id { get; set; }

        // Λίστα ονομάτων μαθημάτων που διδάσκονται σε αυτό το εξάμηνο
        [JsonPropertyName("subjects")]
        public List<string> Subjects { get; set; } = new List<string>();
    }

    // Κλάση που περιέχει τα μαθήματα όλων των εξαμήνων
    // Χρησιμοποιείται για φόρτωση του προγράμματος σπουδών από JSON
    public class SubjectsData
    {
        // Λίστα με τα δεδομένα μαθημάτων για κάθε εξάμηνο
        [JsonPropertyName("semesters")]
        public List<SemesterSubjects> Semesters { get; set; } = new List<SemesterSubjects>();
    }
}