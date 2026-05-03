using System.ComponentModel.DataAnnotations;

namespace SkiRental.App.Services;

public class AuthenticationService
{
    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string? CurrentUserId { get; private set; }
    public UserRole CurrentRole { get; private set; } = UserRole.Customer;

    public event Action? AuthenticationChanged;

    // Simulate user database - in real app, this would connect to a database
    private readonly Dictionary<string, (string Password, string UserId, string Username, UserRole Role)> _users = new()
    {
        { "admin", ("password", "1", "Administrator", UserRole.Worker) },
        { "worker", ("worker123", "2", "Pracownik", UserRole.Worker) },
        { "user", ("user123", "3", "Klient", UserRole.Customer) },
        { "test@example.com", ("test123", "4", "Test Email User", UserRole.Customer) }
    };

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
                if (user.Password == password)
                {
                    // Successful login
                    IsAuthenticated = true;
                    CurrentUsername = user.Username;
                    CurrentUserId = user.UserId;
                    CurrentRole = user.Role;

                    // Notify UI components of authentication change
                    AuthenticationChanged?.Invoke();

                    // In a real app, you would:
                    // 1. Generate a JWT token
                    // 2. Store it securely (localStorage, secure cookie, etc.)
                    // 3. Set up user session
                    // 4. Handle "remember me" functionality

                    return new LoginResult(true, "Logowanie pomyślne");
                }
            }

            return new LoginResult(false, "Nieprawidłowa nazwa użytkownika lub hasło");
        }
        catch (Exception ex)
        {
            return new LoginResult(false, $"Wystąpił błąd podczas logowania: {ex.Message}");
        }
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentUsername = null;
        CurrentUserId = null;
        CurrentRole = UserRole.Customer;

        // Notify UI components of authentication change
        AuthenticationChanged?.Invoke();

        // In a real app, you would:
        // 1. Clear stored tokens
        // 2. Invalidate session
        // 3. Clear any user data
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
            _users[username.ToLower()] = (password, newUserId, username, UserRole.Customer);

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

public record LoginResult(bool Success, string Message);

public record RegisterResult(bool Success, string Message);
