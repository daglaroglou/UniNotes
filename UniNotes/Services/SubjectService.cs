using System.Text.Json;
using UniNotes.Models;

namespace UniNotes.Services
{
    // Υπηρεσία διαχείρισης μαθημάτων που φορτώνει δεδομένα από αρχείο JSON
    public class SubjectService
    {
        private readonly IWebHostEnvironment _environment;
        private SubjectsData? _subjectsData; // Cache δεδομένων μαθημάτων

        public SubjectService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Ανάκτηση όλων των δεδομένων μαθημάτων (με χρήση cache)
        public async Task<SubjectsData> GetSubjectsDataAsync()
        {
            // Επιστροφή από cache αν υπάρχει ήδη
            if (_subjectsData != null)
            {
                return _subjectsData;
            }

            // Διαδρομή αρχείου JSON μαθημάτων
            string filePath = Path.Combine(_environment.WebRootPath, "subjects.json");

            // Έλεγχος αν υπάρχει το αρχείο
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Subjects file not found at {filePath}");
            }

            // Άνοιγμα του αρχείου για ανάγνωση
            using FileStream stream = File.OpenRead(filePath);

            // Ρύθμιση επιλογών JSON ώστε να μην γίνεται διάκριση πεζών-κεφαλαίων
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Αποσειριοποίηση δεδομένων από το αρχείο JSON
            _subjectsData = await JsonSerializer.DeserializeAsync<SubjectsData>(stream, options);

            // Επιστροφή των δεδομένων ή κενού αντικειμένου σε περίπτωση αποτυχίας
            return _subjectsData ?? new SubjectsData();
        }

        // Ανάκτηση των μαθημάτων για συγκεκριμένο εξάμηνο
        public async Task<List<string>> GetSubjectsForSemesterAsync(int semesterId)
        {
            // Φόρτωση όλων των δεδομένων μαθημάτων
            var data = await GetSubjectsDataAsync();

            // Εύρεση του συγκεκριμένου εξαμήνου
            var semester = data.Semesters.FirstOrDefault(s => s.Id == semesterId);

            // Επιστροφή μαθημάτων του εξαμήνου ή κενής λίστας αν δεν βρέθηκε
            return semester?.Subjects ?? new List<string>();
        }

        // Αποθήκευση δεδομένων μαθημάτων πίσω στο αρχείο JSON (για διαχειριστές)
        public async Task<bool> SaveSubjectsDataAsync(SubjectsData subjectsData)
        {
            try
            {
                // Διαδρομή αρχείου JSON
                string filePath = Path.Combine(_environment.WebRootPath, "subjects.json");

                // Ρύθμιση επιλογών JSON για ωραία μορφοποίηση
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                // Δημιουργία ή αντικατάσταση του αρχείου
                using FileStream stream = File.Create(filePath);

                // Σειριοποίηση δεδομένων στο αρχείο
                await JsonSerializer.SerializeAsync(stream, subjectsData, options);

                // Ενημέρωση της προσωρινής μνήμης
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