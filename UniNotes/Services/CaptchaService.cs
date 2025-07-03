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
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789";

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
            if (string.IsNullOrEmpty(captchaId)) return Task.FromResult(false);
            if (string.IsNullOrEmpty(userInput)) return Task.FromResult(false);

            if (_cache.TryGetValue(captchaId, out string? expectedText) && !string.IsNullOrEmpty(expectedText))
            {
                _cache.Remove(captchaId);
                return Task.FromResult(
                    string.Equals(userInput, expectedText)
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
                // Fill background with random noise
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte noise = (byte)_random.Next(30, 50);
                        bitmap.SetPixel(x, y, new SKColor(noise, noise, noise));
                    }
                }

                // Draw border
                using (var borderPaint = new SKPaint())
                {
                    borderPaint.Color = new SKColor(60, 60, 60);
                    borderPaint.StrokeWidth = 2;
                    borderPaint.Style = SKPaintStyle.Stroke;
                    borderPaint.IsAntialias = true;
                    canvas.DrawRect(new SKRect(1, 1, width - 1, height - 1), borderPaint);
                }

                // Draw heavy background noise
                using (var noisePaint = new SKPaint())
                {
                    noisePaint.Style = SKPaintStyle.Fill;
                    for (int i = 0; i < 500; i++)
                    {
                        byte alpha = (byte)_random.Next(40, 100);
                        noisePaint.Color = new SKColor(
                            (byte)_random.Next(150, 220),
                            (byte)_random.Next(150, 220),
                            (byte)_random.Next(150, 220),
                            alpha
                        );
                        
                        float x = _random.Next(width);
                        float y = _random.Next(height);
                        float radius = _random.Next(1, 3) / 2f;
                        canvas.DrawCircle(x, y, radius, noisePaint);
                    }
                }

                // Draw calmer wavy lines
                using (var wavePaint = new SKPaint())
                {
                    wavePaint.Color = new SKColor(150, 150, 150, 180);
                    wavePaint.StrokeWidth = 1.5f;
                    wavePaint.Style = SKPaintStyle.Stroke;
                    wavePaint.IsAntialias = true;

                    for (int i = 0; i < 4; i++)
                    {
                        var path = new SKPath();
                        // Start near the center (middle third of the height)
                        int startY = _random.Next(height / 3, height * 2 / 3);
                        path.MoveTo(0, startY);

                        for (int x = 5; x < width; x += 5)
                        {
                            // Keep endY within a smaller range around the center
                            int endY = startY + _random.Next(-10, 10); // Max ±10px change from current position
                            endY = Math.Clamp(endY, height / 4, height * 3 / 4); // Keep within middle half of height
                            
                            // Reduced control point variation
                            int ctrlY = (startY + endY) / 2 + _random.Next(-3, 3);
                            path.QuadTo(x - 2.5f, ctrlY, x, endY);
                            startY = endY;
                        }
                        canvas.DrawPath(path, wavePaint);
                    }
                }
                // Draw random scratches
                using (var scratchPaint = new SKPaint())
                {
                    for (int i = 0; i < 20; i++)
                    {
                        scratchPaint.Color = new SKColor(
                            (byte)_random.Next(100, 200),
                            (byte)_random.Next(100, 200),
                            (byte)_random.Next(100, 200),
                            (byte)_random.Next(80, 180)
                        );
                        scratchPaint.StrokeWidth = _random.Next(1, 3) / 2f;
                        scratchPaint.Style = SKPaintStyle.Stroke;
                        scratchPaint.IsAntialias = true;

                        float x1 = _random.Next(width);
                        float y1 = _random.Next(height);
                        float x2 = x1 + _random.Next(-20, 20);
                        float y2 = y1 + _random.Next(-10, 10);
                        canvas.DrawLine(x1, y1, x2, y2, scratchPaint);
                    }
                }

                // Draw text with reduced distortion
                using (var font = new SKFont())
                {
                    font.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                    font.Size = 26;
                    font.Subpixel = true;

                    using (var textPaint = new SKPaint())
                    {
                        textPaint.IsAntialias = true;

                        // Measure total text width with increased spacing
                        float totalWidth = 0;
                        float[] charWidths = new float[captchaText.Length];
                        for (int i = 0; i < captchaText.Length; i++)
                        {
                            charWidths[i] = font.MeasureText(captchaText[i].ToString());
                            totalWidth += charWidths[i];
                        }
                        
                        float baseSpacing = 3.5f;
                        totalWidth += baseSpacing * (captchaText.Length - 1);

                        float startX = (width - totalWidth) / 2;
                        float centerY = height / 2f;

                        for (int i = 0; i < captchaText.Length; i++)
                        {
                            string ch = captchaText[i].ToString();
                            float charWidth = charWidths[i];

                            // Get character metrics
                            SKRect bounds = new SKRect();
                            font.MeasureText(ch, out bounds);
                            float baseline = centerY - (bounds.Top + bounds.Bottom) / 2;
                            
                            // Vertical positioning
                            float waveOffset = 2.5f * (float)Math.Sin(i * 0.8f);
                            float posY = baseline + waveOffset + _random.Next(-2, 3);

                            // Set random bright color
                            textPaint.Color = GetRandomBrightColor();

                            // Draw character with reduced distortion
                            canvas.Save();
                            
                            // Horizontal jitter
                            float jitterX = _random.Next(-2, 3);
                            canvas.Translate(startX + jitterX, posY);
                            
                            // Rotation
                            canvas.RotateDegrees(_random.Next(-10, 10));
                            
                            // Skew
                            float skewX = _random.Next(-10, 10) / 25f;
                            float skewY = _random.Next(-5, 6) / 25f;
                            canvas.Skew(skewX, skewY);
                            
                            // Scale
                            float scaleX = 1 + (_random.Next(-15, 16) / 100f);
                            float scaleY = 1 + (_random.Next(-10, 11) / 100f);
                            canvas.Scale(scaleX, scaleY);
                            
                            canvas.DrawText(ch, 0, 0, SKTextAlign.Left, font, textPaint);
                            canvas.Restore();

                            // Increased spacing with more jitter
                            startX += charWidth + baseSpacing + _random.Next(-1, 3);
                        }
                    }
                }

                // Keep heavy foreground grain (over text)
                using (var grainPaint = new SKPaint())
                {
                    grainPaint.Style = SKPaintStyle.Fill;
                    for (int i = 0; i < 1000; i++)
                    {
                        byte gray = (byte)_random.Next(150, 255);
                        byte alpha = (byte)_random.Next(40, 60);
                        grainPaint.Color = new SKColor(gray, gray, gray, alpha);
                        
                        float x = _random.Next(width);
                        float y = _random.Next(height);
                        float radius = _random.Next(1, 3) / 3f;
                        canvas.DrawCircle(x, y, radius, grainPaint);
                    }
                }

                // Add final scratches
                using (var topScratchPaint = new SKPaint())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        topScratchPaint.Color = new SKColor(
                            (byte)_random.Next(150, 255),
                            (byte)_random.Next(150, 255),
                            (byte)_random.Next(150, 255),
                            (byte)_random.Next(50, 150)
                        );
                        topScratchPaint.StrokeWidth = _random.Next(1, 3) / 2f;
                        topScratchPaint.Style = SKPaintStyle.Stroke;
                        topScratchPaint.IsAntialias = true;

                        float x1 = _random.Next(width);
                        float y1 = _random.Next(height);
                        float x2 = x1 + _random.Next(-25, 26);
                        float y2 = y1 + _random.Next(-15, 16);
                        canvas.DrawLine(x1, y1, x2, y2, topScratchPaint);
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