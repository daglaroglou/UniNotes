using System.Text.RegularExpressions;

namespace UniNotes.Funcs
{
    public class Misc
    {
        public static bool IsValidEmail(string email)
        {
            return new Regex(@"(ics|iis)24[0-9]{3}@uom\.edu\.gr", RegexOptions.IgnoreCase).IsMatch(email);
        }

        public static bool IsValidUniID(string uniID)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(uniID, @"^[0-9]{12}$");
        }
    }
}
