using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using UniNotes.Components;
using UniNotes.Services;

// Αρχικό σημείο εισόδου της εφαρμογής Blazor Server
// Εδώ καταχωρούνται οι υπηρεσίες και διαμορφώνεται η διοχέτευση αιτημάτων HTTP

var builder = WebApplication.CreateBuilder(args);

// Καταχώρηση των βασικών υπηρεσιών Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Καταχώρηση υπηρεσιών αποθήκευσης συνεδρίας και παρόχου κατάστασης αυθεντικοποίησης
builder.Services.AddScoped<ProtectedLocalStorage>();      // Προστατευμένη τοπική αποθήκευση για δεδομένα συνεδρίας
builder.Services.AddScoped<AuthenticationStateProvider, BlazorAuthStateProvider>(); // Παροχέας κατάστασης αυθεντικοποίησης

// Καταχώρηση των βασικών υπηρεσιών της εφαρμογής
builder.Services.AddScoped<UserService>();     // Υπηρεσία διαχείρισης χρηστών
builder.Services.AddScoped<SubjectService>();  // Υπηρεσία διαχείρισης μαθημάτων

// Καταχώρηση υπηρεσίας διαχείρισης σημειώσεων
builder.Services.AddScoped<NoteService>();

// Καταχώρηση HttpClient για κλήσεις API
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<GitHubService>(); // Παραμετροποιημένος HttpClient για το GitHub API
builder.Services.AddScoped<GitHubService>();     // Υπηρεσία για επικοινωνία με το GitHub API

// Καταχώρηση υπηρεσιών εξουσιοδότησης
builder.Services.AddAuthorizationCore();

// Καταχώρηση υπηρεσιών προσωρινής μνήμης και CAPTCHA
builder.Services.AddMemoryCache();                         // Προσωρινή μνήμη για αποθήκευση δεδομένων CAPTCHA
builder.Services.AddScoped<ICaptchaService, CaptchaService>(); // Υπηρεσία δημιουργίας και επαλήθευσης CAPTCHA

// Δημιουργία της εφαρμογής από τον builder
var app = builder.Build();

// Διαμόρφωση της διοχέτευσης αιτημάτων HTTP
if (!app.Environment.IsDevelopment())
{
    // Ρυθμίσεις για περιβάλλον παραγωγής
    app.UseExceptionHandler("/Error", createScopeForErrors: true); // Χειριστής σφαλμάτων
    // Η προεπιλεγμένη τιμή HSTS είναι 30 ημέρες. Μπορείτε να την τροποποιήσετε για σενάρια παραγωγής
    app.UseHsts(); // Ενεργοποίηση HTTP Strict Transport Security
}

// Ενεργοποίηση στατικών αρχείων (CSS, JavaScript, εικόνες)
app.UseStaticFiles();

// Ενεργοποίηση δρομολόγησης
app.UseRouting();

// Ανακατεύθυνση από HTTP σε HTTPS για ασφάλεια
app.UseHttpsRedirection();

// Ενεργοποίηση προστασίας από cross-site request forgery (CSRF)
app.UseAntiforgery();

// Χαρτογράφηση στατικών περιουσιακών στοιχείων
app.MapStaticAssets();

// Χαρτογράφηση των Razor Components και προσθήκη λειτουργίας διαδραστικής απόδοσης
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Εκκίνηση της εφαρμογής
app.Run();