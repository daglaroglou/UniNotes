using System.Text.RegularExpressions;
using System.Linq;

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
            return Regex.IsMatch(uniID, @"^[0-9]{12}$");
        }

        public static bool IsValidUsername(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,}$");
        }

        public static bool IsValidPassword(string password)
        {
            return password.Length >= 8;
        }
    }
}
