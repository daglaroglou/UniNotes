using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace UniNotes.Services
{
    // Υπηρεσία που διαχειρίζεται τη δημιουργία και επικύρωση CAPTCHA
    // Χρησιμοποιείται στη φόρμα εγγραφής για προστασία από bots
    public class CaptchaService : ICaptchaService
    {
        private readonly IMemoryCache _cache;
        private readonly Random _random = new Random();
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Χαρακτήρες για το CAPTCHA (εξαιρούνται εύκολα συγχεόμενοι χαρακτήρες)

        public CaptchaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Δημιουργεί ένα νέο CAPTCHA με τυχαίο κείμενο και εικόνα
        public Task<CaptchaResult> GenerateCaptchaAsync()
        {
            // Δημιουργία τυχαίου κωδικού CAPTCHA 6 χαρακτήρων
            var captchaText = GenerateCaptchaText(6);

            // Δημιουργία μοναδικού αναγνωριστικού για αυτό το CAPTCHA
            var captchaId = Guid.NewGuid().ToString();

            // Αποθήκευση του κειμένου CAPTCHA στην cache με το ID ως κλειδί (λήξη μετά από 10 λεπτά)
            _cache.Set(captchaId, captchaText, TimeSpan.FromMinutes(10));

            // Δημιουργία εικόνας απευθείας ως συμβολοσειρά base64
            string imageDataUrl = GenerateCaptchaImageAsDataUrl(captchaText);

            return Task.FromResult(new CaptchaResult
            {
                CaptchaId = captchaId,
                ImageUrl = imageDataUrl
            });
        }

        // Επικυρώνει την είσοδο του χρήστη για ένα συγκεκριμένο CAPTCHA
        public Task<bool> ValidateCaptchaAsync(string captchaId, string userInput)
        {
            // Προσπάθεια λήψης του αναμενόμενου κειμένου CAPTCHA από την cache
            if (_cache.TryGetValue(captchaId, out string expectedText))
            {
                // Αφαίρεση του CAPTCHA από την cache για αποτροπή επαναχρησιμοποίησης
                _cache.Remove(captchaId);

                // Σύγκριση της εισόδου χρήστη με το αναμενόμενο κείμενο (χωρίς διάκριση πεζών-κεφαλαίων)
                return Task.FromResult(string.Equals(userInput, expectedText, StringComparison.OrdinalIgnoreCase));
            }

            // Αν το captchaId δεν βρέθηκε στην cache ή έχει λήξει
            return Task.FromResult(false);
        }

        // Δημιουργεί τυχαίο κείμενο για το CAPTCHA με το καθορισμένο μήκος
        private string GenerateCaptchaText(int length)
        {
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = Chars[_random.Next(Chars.Length)];
            }

            return new string(result);
        }

        // Δημιουργεί την εικόνα του CAPTCHA ως URL δεδομένων (data URL) με βάση το παρεχόμενο κείμενο
        private string GenerateCaptchaImageAsDataUrl(string captchaText)
        {
            // Διαστάσεις για την εικόνα CAPTCHA
            int width = 220;
            int height = 60;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Ρύθμιση για απόδοση υψηλής ποιότητας
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Γέμισμα φόντου με σκούρο χρώμα που ταιριάζει με το θέμα του ιστότοπου
                using (var bgBrush = new SolidBrush(Color.FromArgb(35, 35, 35)))
                {
                    g.FillRectangle(bgBrush, 0, 0, width, height);
                }

                // Προσθήκη διακριτικού περιγράμματος
                using (var borderPen = new Pen(Color.FromArgb(60, 60, 60), 2))
                {
                    g.DrawRectangle(borderPen, 1, 1, width - 3, height - 3);
                }

                // Προσθήκη θορύβου (κουκίδες)
                for (int i = 0; i < 40; i++)
                {
                    int x = _random.Next(width);
                    int y = _random.Next(height);
                    int size = _random.Next(1, 3);
                    using (var noiseBrush = new SolidBrush(Color.FromArgb(80, 200, 200, 200)))
                    {
                        g.FillEllipse(noiseBrush, x, y, size, size);
                    }
                }

                // Προσθήκη διακριτικών κυματιστών γραμμών
                for (int i = 0; i < 2; i++)
                {
                    Point[] points = new Point[width / 10];
                    for (int x = 0; x < points.Length; x++)
                    {
                        points[x] = new Point(
                            x * 10,
                            height / 2 + _random.Next(-height / 6, height / 6)
                        );
                    }

                    using (Pen pen = new Pen(Color.FromArgb(60, 180, 180, 180), 1))
                    {
                        g.DrawCurve(pen, points, 0.5f);
                    }
                }

                // Ρυθμίσεις γραμματοσειράς
                using (Font font = new Font("Arial", 26, FontStyle.Bold))
                {
                    // Προϋπολογισμός πλατών χαρακτήρων για πιο ακριβή διαστήματα
                    float[] charWidths = new float[captchaText.Length];
                    float totalWidth = 0;

                    for (int i = 0; i < captchaText.Length; i++)
                    {
                        SizeF charSize = g.MeasureString(captchaText[i].ToString(), font);
                        charWidths[i] = charSize.Width;
                        totalWidth += charSize.Width - 2; // Υπολογισμός για επικάλυψη
                    }

                    // Προσθήκη διαστήματος μεταξύ χαρακτήρων στο τέλος
                    totalWidth += (captchaText.Length - 1) * 2;

                    // Υπολογισμός αρχικής θέσης για κεντράρισμα του κειμένου
                    float startX = (width - totalWidth) / 2;
                    float centerY = height / 2;

                    // Σχεδίαση χαρακτήρων - ΚΕΝΤΡΑΡΙΣΜΕΝΟΙ στην εικόνα
                    float currentX = startX;

                    for (int i = 0; i < captchaText.Length; i++)
                    {
                        // Υπολογισμός θέσης με ελάχιστη τυχαιότητα για διατήρηση κεντραρίσματος
                        float posX = currentX;
                        float posY = centerY - (font.Height / 2) + _random.Next(-3, 3);

                        // Αποθήκευση κατάστασης
                        Matrix originalTransform = g.Transform.Clone();

                        // Εφαρμογή μετασχηματισμών
                        g.TranslateTransform(posX, posY);
                        g.RotateTransform(_random.Next(-8, 8)); // Λιγότερη περιστροφή για καλύτερη αναγνωσιμότητα

                        // Σχεδίαση με φωτεινό χρώμα
                        using (Brush brush = new SolidBrush(GetRandomBrightColor()))
                        {
                            g.DrawString(captchaText[i].ToString(), font, brush, 0, 0);
                        }

                        // Επαναφορά αρχικού μετασχηματισμού
                        g.Transform = originalTransform;

                        // Μετακίνηση θέσης για επόμενο χαρακτήρα με συνεπή διαστήματα
                        currentX += charWidths[i] - 2 + 2; // Πλάτος χαρακτήρα μείον επικάλυψη συν διάστημα
                    }
                }

                // Μετατροπή του bitmap σε συμβολοσειρά Base64
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    byte[] imageBytes = memoryStream.ToArray();
                    string base64String = Convert.ToBase64String(imageBytes);

                    // Επιστροφή ως data URL που μπορεί να χρησιμοποιηθεί απευθείας στο χαρακτηριστικό src ενός στοιχείου <img>
                    return $"data:image/png;base64,{base64String}";
                }
            }
        }

        // Βοηθητική μέθοδος για δημιουργία χρωμάτων που ταιριάζουν με το θέμα του ιστότοπου
        private Color GetRandomBrightColor()
        {
            // Επιλογή από ένα σύνολο χρωμάτων που ταιριάζουν με το θέμα του ιστότοπου και είναι εύκολα αναγνώσιμα
            Color[] colors = new Color[]
            {
                Color.FromArgb(255, 255, 255),    // Λευκό
                Color.FromArgb(180, 180, 255),    // Ανοιχτό Μπλε
                Color.FromArgb(255, 180, 180),    // Ανοιχτό Κόκκινο
                Color.FromArgb(180, 255, 180),    // Ανοιχτό Πράσινο
                Color.FromArgb(255, 255, 180),    // Ανοιχτό Κίτρινο
            };

            return colors[_random.Next(colors.Length)];
        }
    }
}