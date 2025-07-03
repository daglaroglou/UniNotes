using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

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
            var captchaText = GenerateCaptchaText(6);
            var captchaId = Guid.NewGuid().ToString();
            _cache.Set(captchaId, captchaText, TimeSpan.FromMinutes(10));
            string imageDataUrl = GenerateCaptchaImageAsDataUrl(captchaText);
            return Task.FromResult(new CaptchaResult
            {
                CaptchaId = captchaId,
                ImageUrl = imageDataUrl
            });
        }

        public Task<bool> ValidateCaptchaAsync(string captchaId, string userInput)
        {
            if (_cache.TryGetValue(captchaId, out string expectedText))
            {
                _cache.Remove(captchaId);
                return Task.FromResult(
                    string.Equals(userInput, expectedText, StringComparison.OrdinalIgnoreCase)
                );
            }
            return Task.FromResult(false);
        }

        private string GenerateCaptchaText(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = Chars[_random.Next(Chars.Length)];
            return new string(result);
        }

        private string GenerateCaptchaImageAsDataUrl(string captchaText)
        {
            const int width = 220;
            const int height = 60;

            using (var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul))
            using (var canvas = new SKCanvas(bitmap))
            {
                // Fill background
                canvas.Clear(new SKColor(35, 35, 35));

                // Draw border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.Color = new SKColor(60, 60, 60);
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.IsAntialias = true;
                    canvas.DrawRect(new SKRect(1, 1, width - 1, height - 1), borderPaint);
                }

                // Draw noise (dots)
                using (var noisePaint = new SKPaint())
                {
                    noisePaint.Color = new SKColor(200, 200, 200, 80);
                    noisePaint.Style = SKPaintStyle.Fill;
                    for (int i = 0; i < 40; i++)
                    {
                        float x = _random.Next(width);
                        float y = _random.Next(height);
                        float radius = _random.Next(1, 3) / 2f;
                        canvas.DrawCircle(x, y, radius, noisePaint);
                    }
                }

                // Draw wavy lines --- using quadratic Beziers
                using (var wavePaint = new SKPaint())
                {
                    wavePaint.Color = new SKColor(180, 180, 180, 180);
                    wavePaint.StrokeWidth = 1;
                    wavePaint.Style = SKPaintStyle.Stroke;
                    wavePaint.IsAntialias = true;

                    for (int i = 0; i < 2; i++)
                    {
                        var path = new SKPath();
                        int startY = _random.Next(height / 3, 2 * height / 3);
                        path.MoveTo(0, startY);

                        for (int x = 10; x < width; x += 10)
                        {
                            int endY = _random.Next(height / 3, 2 * height / 3);
                            int ctrlY = (startY + endY) / 2 + _random.Next(-5, 5);
                            path.QuadTo(x - 5, ctrlY, x, endY);
                            startY = endY;
                        }
                        canvas.DrawPath(path, wavePaint);
                    }
                }

                // Draw text
                using (var textPaint = new SKPaint())
                {
                    textPaint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                    textPaint.TextSize = 26;
                    textPaint.IsAntialias = true;
                    textPaint.SubpixelText = true;

                    // Measure total text width
                    float totalWidth = 0;
                    float[] charWidths = new float[captchaText.Length];
                    for (int i = 0; i < captchaText.Length; i++)
                    {
                        charWidths[i] = textPaint.MeasureText(captchaText[i].ToString());
                        totalWidth += charWidths[i] - 2; // Adjust for kerning
                    }
                    totalWidth += 2 * (captchaText.Length - 1); // Add spacing

                    float startX = (width - totalWidth) / 2;
                    float centerY = height / 2f;

                    for (int i = 0; i < captchaText.Length; i++)
                    {
                        string ch = captchaText[i].ToString();
                        float charWidth = charWidths[i] - 2;

                        // Get character metrics
                        SKRect bounds = new SKRect();
                        textPaint.MeasureText(ch, ref bounds);
                        float baseline = centerY - (bounds.Top + bounds.Bottom) / 2;
                        float posY = baseline + _random.Next(-3, 3);

                        // Set random bright color
                        textPaint.Color = GetRandomBrightColor();

                        // Draw character with rotation
                        canvas.Save();
                        canvas.Translate(startX, posY);
                        canvas.RotateDegrees(_random.Next(-8, 8));
                        canvas.DrawText(ch, 0, 0, textPaint);
                        canvas.Restore();

                        startX += charWidth + 2;
                    }
                }

                // Convert to base64 data URL
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return $"data:image/png;base64,{Convert.ToBase64String(data.ToArray())}";
                }
            }
        }

        private SKColor GetRandomBrightColor()
        {
            SKColor[] colors = {
                new SKColor(255, 255, 255),    // White
                new SKColor(180, 180, 255),    // Light Blue
                new SKColor(255, 180, 180),    // Light Red
                new SKColor(180, 255, 180),    // Light Green
                new SKColor(255, 255, 180),    // Light Yellow
            };
            return colors[_random.Next(colors.Length)];
        }
    }
}