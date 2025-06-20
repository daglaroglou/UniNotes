using System.Threading.Tasks;

namespace UniNotes.Services
{
    // Διεπαφή που ορίζει τη λειτουργικότητα μιας υπηρεσίας CAPTCHA
    public interface ICaptchaService
    {
        // Δημιουργεί ένα νέο CAPTCHA και επιστρέφει το αναγνωριστικό του και την εικόνα του
        Task<CaptchaResult> GenerateCaptchaAsync();

        // Επικυρώνει την είσοδο του χρήστη για ένα συγκεκριμένο CAPTCHA
        Task<bool> ValidateCaptchaAsync(string captchaId, string userInput);
    }

    // Κλάση που περιέχει το αποτέλεσμα της δημιουργίας ενός CAPTCHA
    public class CaptchaResult
    {
        // Μοναδικό αναγνωριστικό του CAPTCHA
        public string CaptchaId { get; set; } = string.Empty;

        // URL της εικόνας του CAPTCHA (συνήθως ως data URL με Base64)
        public string ImageUrl { get; set; } = string.Empty;
    }
}