namespace UniNotes.Funcs
{
    public class Misc
    {
        public static bool IsValidEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static bool IsValidUniID(string uniID)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(uniID, @"^[0-9]{12}$");
        }
    }
}
