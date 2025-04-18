namespace UniNotes.Models
{
    public class Admin : User
    {
        public int? adminID { get; set; }
        public string? role { get; set; } // e.g. "superadmin", "moderator", "reviewer"
    }
}
