using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using UniNotes.Models;

namespace UniNotes.Services
{
    // Παροχέας κατάστασης αυθεντικοποίησης για την εφαρμογή Blazor
    // Διαχειρίζεται τη συνεδρία χρήστη και διατηρεί την κατάσταση σύνδεσης
    public class BlazorAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage; // Χρήση ProtectedLocalStorage για διατήρηση μεταξύ επανεκκινήσεων
        private readonly UserService _userService;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public BlazorAuthStateProvider(
            ProtectedLocalStorage localStorage,
            UserService userService)
        {
            _localStorage = localStorage;
            _userService = userService;
        }

        // Επιστρέφει την τρέχουσα κατάσταση αυθεντικοποίησης του χρήστη
        // Ανακτά τη συνεδρία από το προστατευμένο τοπικό αποθηκευτήριο αν υπάρχει
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSessionStorageResult = await _localStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;

                if (userSession == null)
                {
                    return new AuthenticationState(_anonymous);
                }

                var user = await _userService.GetUserByIdAsync(userSession.UserId);
                if (user == null)
                {
                    return new AuthenticationState(_anonymous);
                }

                var claimsPrincipal = CreateClaimsPrincipal(user);
                return new AuthenticationState(claimsPrincipal);
            }
            catch
            {
                return new AuthenticationState(_anonymous);
            }
        }

        // Ενημερώνει την κατάσταση αυθεντικοποίησης με νέα δεδομένα χρήστη
        // Καλείται κατά τη σύνδεση ή αποσύνδεση του χρήστη
        public async Task UpdateAuthenticationState(User? user)
        {
            ClaimsPrincipal claimsPrincipal;

            if (user != null)
            {
                var userSession = new UserSession { UserId = user.Id };
                await _localStorage.SetAsync("UserSession", userSession);
                claimsPrincipal = CreateClaimsPrincipal(user);
            }
            else
            {
                await _localStorage.DeleteAsync("UserSession");
                claimsPrincipal = _anonymous;
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        // Δημιουργεί ένα ClaimsPrincipal με τα στοιχεία του χρήστη για αυθεντικοποίηση
        private ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            };

            var identity = new ClaimsIdentity(claims, "UniNotesAuth");
            return new ClaimsPrincipal(identity);
        }
    }

    // Κλάση που αντιπροσωπεύει μια συνεδρία χρήστη που αποθηκεύεται στο τοπικό αποθηκευτήριο
    public class UserSession
    {
        public required string UserId { get; set; }
    }
}
