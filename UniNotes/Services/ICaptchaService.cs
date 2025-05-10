using System.Threading.Tasks;

namespace UniNotes.Services
{
    public interface ICaptchaService
    {
        Task<CaptchaResult> GenerateCaptchaAsync();
        Task<bool> ValidateCaptchaAsync(string captchaId, string userInput);
    }

    public class CaptchaResult
    {
        public string CaptchaId { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}