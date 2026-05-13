using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;

namespace SkiRental.App.Services;

/// <summary>
/// Authentication service backed by the <see cref="SkiRental.Logic.Interfaces.IUserRepository"/>
/// (EF Core / SQLite). Replaces the previous in-memory dictionary implementation.
///
/// Registered as Singleton in MAUI DI — holds the current session state.
/// Uses IServiceScopeFactory to create scoped DbContext on each operation
/// (avoids the Singleton → Scoped lifetime conflict).
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JwtTokenService      _jwtTokenService;
    private readonly ILogger<AuthenticationService> _logger;

    // ── Session state (in-memory, valid for the lifetime of the app process) ──
    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    public int? CurrentUserId { get; private set; }
    public AppRole CurrentRole { get; private set; } = AppRole.Customer;
    public string? CurrentJwtToken { get; private set; }

    /// <summary>Fired whenever login/logout state changes — Blazor components subscribe to this.</summary>
    public event Action? AuthenticationChanged;

    public AuthenticationService(
        IServiceScopeFactory scopeFactory,
        JwtTokenService jwtTokenService,
        ILogger<AuthenticationService> logger)
    {
        _scopeFactory    = scopeFactory;
        _jwtTokenService = jwtTokenService;
        _logger          = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Login
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<LoginResult> LoginAsync(
        string username,
        string password,
        bool rememberMe = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new LoginResult(false, "Nazwa użytkownika i hasło są wymagane.");

        using var scope      = _scopeFactory.CreateScope();
        var userRepo         = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var userService      = scope.ServiceProvider.GetRequiredService<UserService>();

        AppUser? user;
        try
        {
            user = await userRepo.GetByUsernameAsync(username, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error during login for {Username}", username);
            // For debugging, show actual error message
            return new LoginResult(false, $"Błąd bazy danych: {ex.Message}");
        }

        if (user is null)
        {
            _logger.LogWarning("Login failed – unknown user: {Username}", username);
            // Return same message as wrong password to prevent username enumeration
            return new LoginResult(false, "Nieprawidłowa nazwa użytkownika lub hasło.");
        }

        // Lockout check
        if (user.IsCurrentlyLocked)
        {
            var remaining = user.LockoutEnd.HasValue
                ? $" Spróbuj ponownie o {user.LockoutEnd.Value.ToLocalTime():HH:mm}."
                : string.Empty;
            _logger.LogWarning("Login blocked – user {Username} is locked", username);
            return new LoginResult(false, $"Konto jest zablokowane.{remaining}");
        }

        // Password verification
        bool passwordOk;
        try
        {
            passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BCrypt verification error for {Username}", username);
            return new LoginResult(false, "Błąd weryfikacji hasła.");
        }

        if (!passwordOk)
        {
            await userService.RecordFailedLoginAsync(user, ct);
            _logger.LogWarning("Login failed – wrong password for {Username} (attempt {N})",
                username, user.FailedLoginAttempts);
            return new LoginResult(false, "Nieprawidłowa nazwa użytkownika lub hasło.");
        }

        // Success
        await userService.RecordSuccessfulLoginAsync(user, ct);

        var jwtToken = _jwtTokenService.GenerateToken(
            user.Id.ToString(), user.Username, MapToLegacyRole(user.AppRole));

        IsAuthenticated  = true;
        CurrentUsername  = user.Username;
        CurrentUserId    = user.Id;
        CurrentRole      = user.AppRole;
        CurrentJwtToken  = jwtToken;

        _logger.LogInformation("User {Username} logged in (role={Role})", user.Username, user.AppRole);
        AuthenticationChanged?.Invoke();

        // TODO: For "Remember Me" on mobile, persist the JWT token to
        //       SecureStorage (Microsoft.Maui.Storage.SecureStorage) so it
        //       survives app restarts. That requires token refresh logic —
        //       planned for the API-based auth phase.

        return new LoginResult(true, "Logowanie pomyślne.", jwtToken);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Logout
    // ═══════════════════════════════════════════════════════════════════════

    public void Logout()
    {
        _logger.LogInformation("User {Username} logged out", CurrentUsername);

        IsAuthenticated = false;
        CurrentUsername = null;
        CurrentUserId   = null;
        CurrentRole     = AppRole.Customer;
        CurrentJwtToken = null;

        AuthenticationChanged?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Registration (self-service, Customer role only)
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<RegisterResult> RegisterAsync(
        string username,
        string password,
        string confirmPassword,
        CancellationToken ct = default)
    {
        if (password != confirmPassword)
            return new RegisterResult(false, "Hasła nie są identyczne.");

        using var scope  = _scopeFactory.CreateScope();
        var userService  = scope.ServiceProvider.GetRequiredService<UserService>();

        var result = await userService.CreateUserAsync(username, password, AppRole.Customer, ct);
        return result.IsSuccess
            ? new RegisterResult(true, "Rejestracja pomyślna. Możesz się teraz zalogować.")
            : new RegisterResult(false, result.ErrorMessage ?? "Nieznany błąd.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Role helpers (convenience for Blazor components)
    // ═══════════════════════════════════════════════════════════════════════

    public bool IsOwner  => IsAuthenticated && CurrentRole == AppRole.Owner;
    public bool IsWorker => IsAuthenticated && CurrentRole is AppRole.Worker or AppRole.Owner;
    public bool IsCustomer => IsAuthenticated && CurrentRole == AppRole.Customer;

    // ── Backward-compat shim for any components still using old UserRole enum ──
    public UserRole CurrentLegacyRole => MapToLegacyRole(CurrentRole);

    private static UserRole MapToLegacyRole(AppRole role) => role switch
    {
        AppRole.Owner  => UserRole.Worker,   // Owner is a superset of Worker
        AppRole.Worker => UserRole.Worker,
        _              => UserRole.Customer
    };
}

// Keep the old enum for backward compatibility with existing Blazor components.
// New code should use AppRole directly.
public enum UserRole { Customer, Worker }

public record LoginResult(bool Success, string Message, string? JwtToken = null);
public record RegisterResult(bool Success, string Message);
