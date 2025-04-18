using UniNotes.Models;

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

        public static bool IsValidIpAddress(string ipAddress)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ipAddress,
                @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
        }

        public static void UpdateUserLoginInfo(User user, string ipAddress)
        {
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;

                if (IsValidIpAddress(ipAddress))
                {
                    user.LastLoginIp = ipAddress;
                }
            }
        }
    }
}
