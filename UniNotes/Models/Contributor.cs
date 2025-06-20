namespace UniNotes.Models
{
    // Κλάση που αντιπροσωπεύει έναν συνεισφέροντα στο GitHub repository
    // Χρησιμοποιείται για την εμφάνιση της λίστας συνεισφερόντων στη σελίδα Credits
    public class Contributor
    {
        // Το όνομα χρήστη (username) του συνεισφέροντα στο GitHub
        public string login { get; set; } = "";

        // Η διεύθυνση URL της εικόνας προφίλ (avatar) του συνεισφέροντα
        public string avatar_url { get; set; } = "";

        // Ο αριθμός των συνεισφορών (commits, pull requests, issues) του χρήστη στο repository
        public int contributions { get; set; } = 0;
    }
}