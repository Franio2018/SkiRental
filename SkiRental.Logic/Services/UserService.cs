using Microsoft.Extensions.Logging;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Domain service for user and staff management.
/// All mutating operations (create worker, change role, lock) are Owner-only
/// at the UI layer — this service does not enforce that; the calling Blazor
/// component / route guard does.
/// </summary>
public sealed class UserService
{
    private readonly IUserRepository _userRepo;
    private readonly ILogger<UserService> _logger;

    /// <summary>Maximum consecutive failed logins before auto-lockout.</summary>
    private const int MaxFailedAttempts = 5;

    /// <summary>Lockout duration after exceeding max failed attempts.</summary>
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public UserService(IUserRepository userRepo, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _logger   = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Read
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Returns all users (owner staff management panel).</summary>
    public Task<IReadOnlyList<AppUser>> GetAllUsersAsync(CancellationToken ct = default)
        => _userRepo.GetAllAsync(ct);

    /// <summary>Returns a single user by ID.</summary>
    public Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default)
        => _userRepo.GetByIdAsync(id, ct);

    // ═══════════════════════════════════════════════════════════════════════
    // Create
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new user account (registration or owner-created staff account).
    /// </summary>
    public async Task<CreateUserResult> CreateUserAsync(
        string username,
        string password,
        AppRole role,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return CreateUserResult.Failure("Nazwa użytkownika musi mieć co najmniej 3 znaki.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return CreateUserResult.Failure("Hasło musi mieć co najmniej 8 znaków.");

        if (await _userRepo.UsernameExistsAsync(username, ct))
            return CreateUserResult.Failure($"Nazwa użytkownika '{username}' jest już zajęta.");

        var user = new AppUser
        {
            Username     = username.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            AppRole      = role,
            CreatedAt    = DateTime.UtcNow
        };

        user = await _userRepo.CreateAsync(user, ct);

        _logger.LogInformation("User created: {Username} role={Role}", user.Username, role);
        return CreateUserResult.Success(user.Id);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Update
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Changes a user's role. Owner can promote/demote Workers and Customers.
    /// Cannot demote the last Owner account.
    /// </summary>
    public async Task<OperationResult> ChangeRoleAsync(
        int targetUserId,
        AppRole newRole,
        CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(targetUserId, ct);
        if (user is null) return OperationResult.Failure("Użytkownik nie istnieje.");

        // Guard: must always have at least one Owner
        if (user.AppRole == AppRole.Owner && newRole != AppRole.Owner)
        {
            var all = await _userRepo.GetAllAsync(ct);
            if (all.Count(u => u.AppRole == AppRole.Owner) <= 1)
                return OperationResult.Failure("Nie można zdegradować ostatniego właściciela.");
        }

        user.AppRole = newRole;
        await _userRepo.UpdateAsync(user, ct);
        _logger.LogInformation("Role changed for user {Id}: {NewRole}", targetUserId, newRole);
        return OperationResult.Ok();
    }

    /// <summary>Changes a user's password (owner resetting staff, or user changing own).</summary>
    public async Task<OperationResult> ChangePasswordAsync(
        int userId,
        string newPassword,
        CancellationToken ct = default)
    {
        if (newPassword.Length < 8)
            return OperationResult.Failure("Hasło musi mieć co najmniej 8 znaków.");

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return OperationResult.Failure("Użytkownik nie istnieje.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        await _userRepo.UpdateAsync(user, ct);
        _logger.LogInformation("Password changed for user {Id}", userId);
        return OperationResult.Ok();
    }

    /// <summary>Locks or unlocks a user account (Owner action).</summary>
    public async Task<OperationResult> SetLockAsync(
        int userId,
        bool locked,
        CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return OperationResult.Failure("Użytkownik nie istnieje.");

        user.IsLocked          = locked;
        user.LockoutEnd        = locked ? null : null;   // permanent lock when set via owner panel
        user.FailedLoginAttempts = 0;
        await _userRepo.UpdateAsync(user, ct);
        _logger.LogInformation("User {Id} lock set to {Locked}", userId, locked);
        return OperationResult.Ok();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Login attempt tracking (called by AuthenticationService)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Records a failed login attempt and applies automatic lockout
    /// after <see cref="MaxFailedAttempts"/> consecutive failures.
    /// </summary>
    public async Task RecordFailedLoginAsync(AppUser user, CancellationToken ct = default)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= MaxFailedAttempts)
        {
            user.IsLocked   = true;
            user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            _logger.LogWarning("User {Username} auto-locked after {N} failed attempts",
                user.Username, user.FailedLoginAttempts);
        }
        await _userRepo.UpdateAsync(user, ct);
    }

    /// <summary>Resets failed login counter on successful authentication.</summary>
    public async Task RecordSuccessfulLoginAsync(AppUser user, CancellationToken ct = default)
    {
        user.FailedLoginAttempts = 0;
        user.IsLocked            = false;
        user.LockoutEnd          = null;
        user.LastLoginAt         = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Profile
    // ═══════════════════════════════════════════════════════════════════════

    public Task<UserProfile?> GetProfileAsync(int userId, CancellationToken ct = default)
        => _userRepo.GetProfileByUserIdAsync(userId, ct);

    public Task<UserProfile> SaveProfileAsync(int userId, UserProfile profile, CancellationToken ct = default)
        => _userRepo.UpsertProfileAsync(userId, profile, ct);
}

// ═══════════════════════════════════════════════════════════════════════════════
// Result types
// ═══════════════════════════════════════════════════════════════════════════════

public record CreateUserResult
{
    public bool IsSuccess { get; init; }
    public int? UserId { get; init; }
    public string? ErrorMessage { get; init; }

    public static CreateUserResult Success(int id) => new() { IsSuccess = true, UserId = id };
    public static CreateUserResult Failure(string msg) => new() { IsSuccess = false, ErrorMessage = msg };
}

public record OperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static OperationResult Ok() => new() { IsSuccess = true };
    public static OperationResult Failure(string msg) => new() { IsSuccess = false, ErrorMessage = msg };
}
