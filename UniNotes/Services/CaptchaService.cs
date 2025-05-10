using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace UniNotes.Services
{
    public class CaptchaService : ICaptchaService
    {
        private readonly IMemoryCache _cache;
        private readonly Random _random = new Random();
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        
        public CaptchaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<CaptchaResult> GenerateCaptchaAsync()
        {
            // Generate a random 6-character captcha code
            var captchaText = GenerateCaptchaText(6);
            
            // Create a unique ID for this captcha
            var captchaId = Guid.NewGuid().ToString();
            
            // Store the captcha text in cache with the ID as key (expire after 10 minutes)
            _cache.Set(captchaId, captchaText, TimeSpan.FromMinutes(10));
            
            // Generate image directly as base64 string
            string imageDataUrl = GenerateCaptchaImageAsDataUrl(captchaText);
            
            return Task.FromResult(new CaptchaResult
            {
                CaptchaId = captchaId,
                ImageUrl = imageDataUrl
            });
        }

        public Task<bool> ValidateCaptchaAsync(string captchaId, string userInput)
        {
            // Try to get the expected captcha text from cache
            if (_cache.TryGetValue(captchaId, out string expectedText))
            {
                // Remove the captcha from cache to prevent reuse
                _cache.Remove(captchaId);
                
                // Compare the user input with the expected text (case-insensitive)
                return Task.FromResult(string.Equals(userInput, expectedText, StringComparison.OrdinalIgnoreCase));
            }
            
            // If captchaId not found in cache or expired
            return Task.FromResult(false);
        }

        private string GenerateCaptchaText(int length)
        {
            char[] result = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                result[i] = Chars[_random.Next(Chars.Length)];
            }
            
            return new string(result);
        }

        private string GenerateCaptchaImageAsDataUrl(string captchaText)
        {
            // Dimensions for the CAPTCHA image
            int width = 220;
            int height = 60;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Set up for high quality rendering
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Fill background with dark color matching site theme
                using (var bgBrush = new SolidBrush(Color.FromArgb(35, 35, 35)))
                {
                    g.FillRectangle(bgBrush, 0, 0, width, height);
                }

                // Add subtle border
                using (var borderPen = new Pen(Color.FromArgb(60, 60, 60), 2))
                {
                    g.DrawRectangle(borderPen, 1, 1, width - 3, height - 3);
                }

                // Add noise (dots)
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

                // Add subtle wavy lines
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

                // Font settings
                using (Font font = new Font("Arial", 26, FontStyle.Bold))
                {
                    // Pre-calculate character widths for more accurate spacing
                    float[] charWidths = new float[captchaText.Length];
                    float totalWidth = 0;
                    
                    for (int i = 0; i < captchaText.Length; i++)
                    {
                        SizeF charSize = g.MeasureString(captchaText[i].ToString(), font);
                        charWidths[i] = charSize.Width;
                        totalWidth += charSize.Width - 2; // Account for overlap
                    }
                    
                    // Add some spacing between characters at the end
                    totalWidth += (captchaText.Length - 1) * 2;
                    
                    // Calculate starting position to center the entire text
                    float startX = (width - totalWidth) / 2;
                    float centerY = height / 2;
                    
                    // Draw characters - CENTERED in the image
                    float currentX = startX;
                    
                    for (int i = 0; i < captchaText.Length; i++)
                    {
                        // Calculate position with minimal randomization to maintain centering
                        float posX = currentX;
                        float posY = centerY - (font.Height / 2) + _random.Next(-3, 3);
                        
                        // Save state
                        Matrix originalTransform = g.Transform.Clone();
                        
                        // Apply transformations
                        g.TranslateTransform(posX, posY);
                        g.RotateTransform(_random.Next(-8, 8)); // Less rotation for better readability
                        
                        // Draw with bright color
                        using (Brush brush = new SolidBrush(GetRandomBrightColor()))
                        {
                            g.DrawString(captchaText[i].ToString(), font, brush, 0, 0);
                        }
                        
                        // Restore original transformation
                        g.Transform = originalTransform;
                        
                        // Move position for next character with consistent spacing
                        currentX += charWidths[i] - 2 + 2; // Character width minus overlap plus spacing
                    }
                }

                // Convert the bitmap to a Base64 string
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    byte[] imageBytes = memoryStream.ToArray();
                    string base64String = Convert.ToBase64String(imageBytes);
                    
                    // Return as data URL that can be directly used in <img> src attribute
                    return $"data:image/png;base64,{base64String}";
                }
            }
        }
        
        // Helper method to generate colors that match the site's theme
        private Color GetRandomBrightColor()
        {
            // Choose from a set of colors that match the site's theme and are easily readable
            Color[] colors = new Color[]
            {
                Color.FromArgb(255, 255, 255),    // White
                Color.FromArgb(180, 180, 255),    // Light Blue
                Color.FromArgb(255, 180, 180),    // Light Red
                Color.FromArgb(180, 255, 180),    // Light Green
                Color.FromArgb(255, 255, 180),    // Light Yellow
            };
            
            return colors[_random.Next(colors.Length)];
        }
    }
}