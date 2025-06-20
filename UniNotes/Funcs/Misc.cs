using System.Text.RegularExpressions;
using System.Linq;

namespace UniNotes.Funcs
{
    // Κλάση βοηθητικών συναρτήσεων για επικύρωση δεδομένων
    public class Misc
    {
        // Επικυρώνει ότι το email έχει μορφή ακαδημαϊκού λογαριασμού του UoM
        // Δέχεται μόνο διευθύνσεις της μορφής: ics/iis + 5 ψηφία + @uom.edu.gr
        // Παράμετρος email: Η διεύθυνση email προς επικύρωση
        // Επιστρέφει: true αν το email είναι έγκυρο, αλλιώς false
        public static bool IsValidEmail(string email)
        {
            return new Regex(@"(ics|iis)24[0-9]{3}@uom\.edu\.gr", RegexOptions.IgnoreCase).IsMatch(email);
        }

        // Επικυρώνει ότι ο αριθμός φοιτητικής ταυτότητας είναι έγκυρος
        // Δέχεται μόνο 12-ψήφιους αριθμούς
        // Παράμετρος uniID: Ο αριθμός ταυτότητας προς επικύρωση
        // Επιστρέφει: true αν το uniID είναι έγκυρο, αλλιώς false
        public static bool IsValidUniID(string uniID)
        {
            return Regex.IsMatch(uniID, @"^[0-9]{12}$");
        }

        // Επικυρώνει ότι το όνομα χρήστη είναι έγκυρο
        // Επιτρέπει μόνο γράμματα, αριθμούς και κάτω παύλα, με ελάχιστο μήκος 3 χαρακτήρες
        // Παράμετρος username: Το όνομα χρήστη προς επικύρωση
        // Επιστρέφει: true αν το username είναι έγκυρο, αλλιώς false
        public static bool IsValidUsername(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,}$");
        }

        // Επικυρώνει ότι ο κωδικός πρόσβασης είναι έγκυρος
        // Απαιτεί ελάχιστο μήκος 8 χαρακτήρων
        // Παράμετρος password: Ο κωδικός πρόσβασης προς επικύρωση
        // Επιστρέφει: true αν το password είναι έγκυρο, αλλιώς false
        public static bool IsValidPassword(string password)
        {
            return password.Length >= 8;
        }
    }
}