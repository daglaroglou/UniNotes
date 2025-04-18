namespace UniNotes.Models
{
    public class User
    {
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? username { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string? uniID { get; set; }

        private int? _semester;
        public int? semester
        {
            get => _semester;
            set
            {
                _semester = value;
                if (value.HasValue)
                {
                    year = ((value.Value - 1) / 2) + 1;
                }
            }
        }

        public int year { get; set; }
    }
}
