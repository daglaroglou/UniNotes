using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using UniNotes.Models;

namespace UniNotes.Services
{
    public class BlazorAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage; // Use ProtectedLocalStorage instead of ProtectedSessionStorage
        private readonly UniNotes.Services.UserService _userService;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public BlazorAuthStateProvider(
            ProtectedLocalStorage localStorage, // Inject ProtectedLocalStorage
            UserService userService)
        {
            _localStorage = localStorage;
            _userService = userService;
        }

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

        public async Task UpdateAuthenticationState(User? user)
        {
            ClaimsPrincipal claimsPrincipal;

            if (user != null)
            {
                var userSession = new UserSession { UserId = user.Id };
                await _localStorage.SetAsync("UserSession", userSession); // Use localStorage to persist session
                claimsPrincipal = CreateClaimsPrincipal(user);
            }
            else
            {
                await _localStorage.DeleteAsync("UserSession"); // Clear session
                claimsPrincipal = _anonymous;
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

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

    public class UserSession
    {
        public string UserId { get; set; }
    }
}
