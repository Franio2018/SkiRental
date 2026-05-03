using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace SkiRental.App.Services;

public class AuthenticationService
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string? CurrentUserId { get; private set; }
    public UserRole CurrentRole { get; private set; } = UserRole.Customer;
    public string? CurrentJwtToken { get; private set; }

    public event Action? AuthenticationChanged;

    public AuthenticationService(JwtTokenService jwtTokenService, ILogger<AuthenticationService> logger)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    // Simulate user database - in real app, this would connect to a database
    // Passwords are now stored as BCrypt hashes
    private readonly Dictionary<string, (string PasswordHash, string UserId, string Username, UserRole Role)> _users = new()
    {
        { "admin", (HashPassword("password"), "1", "Administrator", UserRole.Worker) },
        { "worker", (HashPassword("worker123"), "2", "Pracownik", UserRole.Worker) },
        { "user", (HashPassword("user123"), "3", "Klient", UserRole.Customer) },
        { "test@example.com", (HashPassword("test123"), "4", "Test Email User", UserRole.Customer) }
    };

    /// <summary>
    /// Helper method to hash passwords using BCrypt
    /// </summary>
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
    {
        try
        {
            // Simulate processing time
            await Task.Delay(1000);

            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult(false, "Nazwa użytkownika i hasło są wymagane");
            }

            // Check credentials
            if (_users.TryGetValue(username.ToLower(), out var user))
            {
                // Verify password using BCrypt
                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    // Generate JWT token
                    var jwtToken = _jwtTokenService.GenerateToken(user.UserId, user.Username, user.Role);
                    
                    // Successful login
                    IsAuthenticated = true;
                    CurrentUsername = user.Username;
                    CurrentUserId = user.UserId;
                    CurrentRole = user.Role;
                    CurrentJwtToken = jwtToken;

                    _logger.LogInformation("User {Username} logged in successfully with JWT token", user.Username);

                    // Notify UI components of authentication change
                    AuthenticationChanged?.Invoke();

                    // In a real app, you would:
                    // 1. Store JWT token securely (localStorage, secure cookie, etc.)
                    // 2. Set up user session
                    // 3. Handle "remember me" functionality
                    // 4. Use token for API calls

                    return new LoginResult(true, "Logowanie pomyślne", jwtToken);
                }
            }

            _logger.LogWarning("Failed login attempt for username: {Username}", username);
            return new LoginResult(false, "Nieprawidłowa nazwa użytkownika lub hasło");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for username: {Username}", username);
            return new LoginResult(false, $"Wystąpił błąd podczas logowania: {ex.Message}");
        }
    }

    public void Logout()
    {
        _logger.LogInformation("User {Username} logged out", CurrentUsername);
        
        IsAuthenticated = false;
        CurrentUsername = null;
        CurrentUserId = null;
        CurrentRole = UserRole.Customer;
        CurrentJwtToken = null;

        // Notify UI components of authentication change
        AuthenticationChanged?.Invoke();

        // In a real app, you would:
        // 1. Clear stored JWT tokens from localStorage/secure storage
        // 2. Invalidate server-side session if applicable
        // 3. Clear any cached user data
        // 4. Notify API endpoints to invalidate token
    }

    public async Task<RegisterResult> RegisterAsync(string username, string email, string password, string confirmPassword)
    {
        try
        {
            // Simulate processing time
            await Task.Delay(500);

            // Validate input
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(username))
                errors.Add("Nazwa użytkownika jest wymagana");

            if (string.IsNullOrWhiteSpace(email))
                errors.Add("Email jest wymagany");
            else if (!email.Contains("@"))
                errors.Add("Nieprawidłowy format email");

            if (string.IsNullOrWhiteSpace(password))
                errors.Add("Hasło jest wymagane");
            else if (password.Length < 6)
                errors.Add("Hasło musi mieć co najmniej 6 znaków");

            if (password != confirmPassword)
                errors.Add("Hasła nie są identyczne");

            if (errors.Any())
            {
                return new RegisterResult(false, string.Join("; ", errors));
            }

            // Check if user already exists
            if (_users.ContainsKey(username.ToLower()) || _users.ContainsKey(email.ToLower()))
            {
                return new RegisterResult(false, "Użytkownik o podanej nazwie lub emailu już istnieje");
            }

            // Register new user (in real app, save to database)
            var newUserId = (_users.Count + 1).ToString();
            var hashedPassword = HashPassword(password);
            _users[username.ToLower()] = (hashedPassword, newUserId, username, UserRole.Customer);

            return new RegisterResult(true, "Rejestracja pomyślna. Możesz się teraz zalogować.");
        }
        catch (Exception ex)
        {
            return new RegisterResult(false, $"Wystąpił błąd podczas rejestracji: {ex.Message}");
        }
    }
}

public enum UserRole
{
    Customer,
    Worker
}

public record LoginResult(bool Success, string Message, string? JwtToken = null);

public record RegisterResult(bool Success, string Message);
