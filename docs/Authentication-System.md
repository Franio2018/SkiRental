# Authentication and Authorization System Documentation

## Overview

The SkiRental application implements a comprehensive authentication and authorization system that provides secure user access control with role-based permissions. The system supports user registration, login/logout functionality, and fine-grained access control to different application features.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Authentication System Architecture                │
├─────────────────────────────────────────────────────────────────┤
│  Presentation Layer                                               │
│  ├── Login Page (Blazor)                                        │
│  ├── Main Layout (Header with User Info)                         │
│  ├── Role-based UI Components                                   │
│  └── Navigation Guards                                           │
├─────────────────────────────────────────────────────────────────┤
│  Service Layer                                                   │
│  ├── AuthenticationService (Core Logic)                             │
│  ├── Session Management                                          │
│  ├── Password Validation                                        │
│  └── Event Broadcasting                                          │
├─────────────────────────────────────────────────────────────────┤
│  Data Layer                                                      │
│  ├── User Store (In-memory/Database)                            │
│  ├── Role Management                                             │
│  └── Audit Logging                                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## User Roles and Permissions

### Role Hierarchy

| Role | Description | Access Level | Permissions |
|------|-------------|--------------|-------------|
| **Customer** | Regular rental customers | Basic | • Create bookings<br>• View own rentals<br>• Manage profile<br>• Basic equipment search |
| **Worker** | Rental shop employees | Advanced | • All Customer permissions<br>• Access QR scanner<br>• View analytics dashboard<br>• Process returns<br>• Manage equipment inventory |
| **Administrator** | System administrators | Full | • All Worker permissions<br>• User management<br>• System configuration<br>• Full analytics access<br>• Audit log access |

### Permission Matrix

| Feature | Customer | Worker | Administrator |
|----------|----------|--------|---------------|
| **Equipment Search** | ✅ | ✅ | ✅ |
| **Create Booking** | ✅ | ✅ | ✅ |
| **View Own Rentals** | ✅ | ✅ | ✅ |
| **Manage Profile** | ✅ | ✅ | ✅ |
| **QR Scanner** | ❌ | ✅ | ✅ |
| **Analytics Dashboard** | ❌ | ✅ | ✅ |
| **User Management** | ❌ | ❌ | ✅ |
| **System Configuration** | ❌ | ❌ | ✅ |

---

## Core Components

### AuthenticationService

**File**: `SkiRental.App\Services/AuthenticationService.cs`

The central service handling all authentication operations.

#### Key Properties

```csharp
public class AuthenticationService
{
    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string? CurrentUserId { get; private set; }
    public UserRole CurrentRole { get; private set; } = UserRole.Customer;
    
    public event Action? AuthenticationChanged;
}
```

#### User Store

```csharp
// Demo user database (in production, use real database)
private readonly Dictionary<string, (string Password, string UserId, string Username, UserRole Role)> _users = new()
{
    { "admin", ("password", "1", "Administrator", UserRole.Worker) },
    { "worker", ("worker123", "2", "Pracownik", UserRole.Worker) },
    { "user", ("user123", "3", "Klient", UserRole.Customer) },
    { "test@example.com", ("test123", "4", "Test Email User", UserRole.Customer) }
};
```

#### Authentication Methods

##### LoginAsync

```csharp
public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
```

**Purpose**: Authenticate user with credentials

**Process**:
1. Input validation (null/empty checks)
2. User lookup by username (case-insensitive)
3. Password verification
4. Session establishment
5. Event notification

**Security Features**:
- Rate limiting (5 attempts per 15 minutes)
- Password comparison with constant-time algorithm
- Session token generation (in production)
- Audit logging

**Example**:
```csharp
var result = await authService.LoginAsync("admin", "password");

if (result.Success)
{
    Console.WriteLine($"Logged in as: {authService.CurrentUsername}");
    Console.WriteLine($"Role: {authService.CurrentRole}");
}
```

##### Logout

```csharp
public void Logout()
```

**Purpose**: Terminate user session and clear authentication state

**Process**:
1. Clear authentication state
2. Invalidate session tokens (in production)
3. Notify UI components
4. Audit logging

**Example**:
```csharp
authService.Logout();
Console.WriteLine($"Is authenticated: {authService.IsAuthenticated}"); // false
```

##### RegisterAsync

```csharp
public async Task<RegisterResult> RegisterAsync(string username, string email, string password, string confirmPassword)
```

**Purpose**: Register new user account

**Validation Rules**:
- Username: Required, max 100 chars, unique
- Email: Required, valid format, unique
- Password: Required, 6-100 chars
- Password confirmation: Must match password

**Example**:
```csharp
var result = await authService.RegisterAsync(
    "newuser", 
    "newuser@example.com", 
    "securepassword123", 
    "securepassword123");

if (result.Success)
{
    Console.WriteLine("Registration successful! Please login.");
}
```

---

## User Interface Components

### LoginPage.razor

**File**: `SkiRental.App\Pages\LoginPage.razor`

The login interface with comprehensive validation and error handling.

#### Features

- **Form Validation**: Real-time input validation with DataAnnotations
- **Password Visibility Toggle**: Show/hide password functionality
- **Error Display**: User-friendly error messages
- **Demo User Information**: Built-in demo credentials
- **Remember Me**: Session persistence option
- **Social Login Options**: Placeholder for future integrations

#### Key Implementation Details

```razor
<EditForm Model="@loginModel" OnValidSubmit="@HandleLogin" class="login-form">
    <DataAnnotationsValidator />
    <ValidationSummary class="error-summary" />
    
    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="error-message">@errorMessage</div>
    }
    
    <!-- Form fields -->
</EditForm>
```

#### Validation Rules

```csharp
public class LoginModel
{
    [Required(ErrorMessage = "Nazwa użytkownika lub email jest wymagany")]
    [StringLength(100, ErrorMessage = "Nazwa użytkownika nie może przekraczać 100 znaków")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Hasło jest wymagane")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć od 6 do 100 znaków")]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}
```

---

### MainLayout.razor

**File**: `SkiRental.App\Shared\MainLayout.razor`

The main layout component with authentication-aware header.

#### Features

- **Dynamic Header**: Changes based on authentication state
- **User Information Display**: Shows username and role
- **Role-based Navigation**: Different options for different roles
- **Logout Functionality**: One-click logout
- **Login Button**: Shown when user is not authenticated

#### Implementation

```razor
<div class="header-actions">
    @if (Auth.IsAuthenticated)
    {
        <div class="user-info">
            <span class="user-name">@Auth.CurrentUsername</span>
            <span class="user-role">@(Auth.CurrentRole == UserRole.Worker ? "Pracownik" : "Klient")</span>
        </div>
        
        <button class="header-btn" title="Powiadomienia">🔔</button>
        <button class="header-btn" title="Profil">👤</button>
        <button class="header-btn logout-btn" title="Wyloguj" @onclick="HandleLogout">🚪</button>
    }
    else
    {
        <button class="header-btn login-btn" title="Zaloguj się" 
                @onclick='() => Navigation.NavigateTo("/login")'>🔑</button>
    }
</div>
```

---

## Role-Based Access Control

### MainPage.xaml.cs

**File**: `SkiRental.App\MainPage.xaml.cs`

Native MAUI page with role-based button visibility control.

#### Permission Checks

```csharp
private async Task NavigateToWithPermissionAsync(string url)
{
    // Check worker permissions for analytics
    if (url == "/analytics" && (!_authService.IsAuthenticated || _authService.CurrentRole != UserRole.Worker))
    {
        await DisplayAlert("Brak uprawnień",
            "Tylko pracownicy mogą przeglądać analitykę.", "OK");
        return;
    }

    await NavigateToAsync(url);
}

private async void OpenScannerAsync(object? sender, EventArgs e)
{
    // Check if user is authenticated and has worker role
    if (!_authService.IsAuthenticated || _authService.CurrentRole != UserRole.Worker)
    {
        await DisplayAlert("Brak uprawnień",
            "Tylko pracownicy mogą używać skanera QR.", "OK");
        return;
    }

    // Proceed with scanner functionality
    // ...
}
```

#### Dynamic UI Updates

```csharp
private void UpdateButtonVisibility()
{
    bool isWorker = _authService.IsAuthenticated && _authService.CurrentRole == UserRole.Worker;

    if (ScannerButton != null)
        ScannerButton.IsVisible = isWorker;

    if (AnalyticsButton != null)
        AnalyticsButton.IsVisible = isWorker;
}

private void OnAuthenticationChanged()
{
    MainThread.BeginInvokeOnMainThread(UpdateButtonVisibility);
}
```

---

### Home.razor

**File**: `SkiRental.App\Pages\Home.razor`

Main dashboard with role-based content display.

#### Conditional Rendering

```razor
@if (Auth.IsAuthenticated && Auth.CurrentRole == UserRole.Worker)
{
    <a href="/employee" class="action-card employee">
        <span class="action-icon">📷</span>
        <h3>Panel Pracownika</h3>
        <p>Skanuj QR / NFC, zarządzaj zwrotami</p>
    </a>
    
    <a href="/analytics" class="action-card analytics">
        <span class="action-icon">📊</span>
        <h3>Analityka</h3>
        <p>Wskaźniki użycia floty – Back-office</p>
    </a>
}
```

---

## Security Implementation

### Password Security

#### Password Storage

```csharp
// In production, use proper password hashing
// Currently using plain text for demo purposes only
private readonly Dictionary<string, (string Password, string UserId, string Username, UserRole Role)> _users = new()
{
    { "admin", ("password", "1", "Administrator", UserRole.Worker) },
    // ... other users
};
```

#### Production Security Recommendations

```csharp
// Use BCrypt.Net for password hashing
private string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}

private bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### Session Management

#### Session State

```csharp
public class AuthenticationService
{
    private string? _sessionToken;
    private DateTime? _sessionExpiry;
    
    private void CreateSession()
    {
        _sessionToken = GenerateSecureToken();
        _sessionExpiry = DateTime.UtcNow.AddHours(24);
        
        // Store session token securely
        // In production: Use secure cookie or mobile secure storage
    }
    
    private bool IsSessionValid()
    {
        return _sessionToken != null && 
               _sessionExpiry.HasValue && 
               _sessionExpiry.Value > DateTime.UtcNow;
    }
}
```

### Rate Limiting

#### Login Attempt Tracking

```csharp
private readonly Dictionary<string, (int Attempts, DateTime LastAttempt)> _loginAttempts = new();

private bool IsRateLimited(string username)
{
    if (_loginAttempts.TryGetValue(username.ToLower(), out var attempt))
    {
        if (attempt.Attempts >= 5 && 
            DateTime.UtcNow - attempt.LastAttempt < TimeSpan.FromMinutes(15))
        {
            return true;
        }
    }
    return false;
}

private void RecordFailedAttempt(string username)
{
    var key = username.ToLower();
    if (_loginAttempts.ContainsKey(key))
    {
        _loginAttempts[key] = (_loginAttempts[key].Attempts + 1, DateTime.UtcNow);
    }
    else
    {
        _loginAttempts[key] = (1, DateTime.UtcNow);
    }
}
```

---

## Event System

### Authentication Events

#### Event Broadcasting

```csharp
public class AuthenticationService
{
    public event Action? AuthenticationChanged;
    
    private void NotifyAuthenticationChanged()
    {
        AuthenticationChanged?.Invoke();
    }
    
    public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
    {
        // ... authentication logic
        
        if (success)
        {
            // Set authentication state
            IsAuthenticated = true;
            CurrentUsername = user.Username;
            CurrentUserId = user.UserId;
            CurrentRole = user.Role;
            
            // Notify subscribers
            NotifyAuthenticationChanged();
        }
        
        return result;
    }
    
    public void Logout()
    {
        // Clear authentication state
        IsAuthenticated = false;
        CurrentUsername = null;
        CurrentUserId = null;
        CurrentRole = UserRole.Customer;
        
        // Notify subscribers
        NotifyAuthenticationChanged();
    }
}
```

#### UI Component Subscriptions

```csharp
// MainPage.xaml.cs
public MainPage(QrNfcBridgeService bridge, AuthenticationService authService)
{
    InitializeComponent();
    _bridge = bridge;
    _authService = authService;
    
    // Subscribe to authentication changes
    _authService.AuthenticationChanged += OnAuthenticationChanged;
    
    UpdateButtonVisibility();
}

private void OnAuthenticationChanged()
{
    MainThread.BeginInvokeOnMainThread(UpdateButtonVisibility);
}
```

---

## Data Models

### UserRole Enum

```csharp
public enum UserRole
{
    Customer = 0,
    Worker = 1,
    Administrator = 2
}
```

### LoginResult

```csharp
public record LoginResult(bool Success, string Message);
```

### RegisterResult

```csharp
public record RegisterResult(bool Success, string Message);
```

### User Session

```csharp
public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string SessionToken { get; set; } = string.Empty;
}
```

---

## Configuration

### Demo User Accounts

| Username | Password | Role | Description |
|----------|----------|------|-------------|
| `admin` | `password` | Worker | System administrator |
| `worker` | `worker123` | Worker | Regular employee |
| `user` | `user123` | Customer | Regular customer |
| `test@example.com` | `test123` | Customer | Email-based customer |

### Environment Configuration

```json
{
  "Authentication": {
    "SessionTimeoutHours": 24,
    "MaxLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "RequireEmailVerification": false,
    "EnableTwoFactorAuth": false,
    "PasswordPolicy": {
      "MinLength": 6,
      "RequireUppercase": false,
      "RequireLowercase": false,
      "RequireNumbers": false,
      "RequireSpecialChars": false
    }
  }
}
```

---

## Testing Authentication

### Unit Tests

#### Login Tests

```csharp
public class AuthenticationServiceTests
{
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _authService = new AuthenticationService();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var username = "admin";
        var password = "password";

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeTrue();
        _authService.IsAuthenticated.Should().BeTrue();
        _authService.CurrentUsername.Should().Be("Administrator");
        _authService.CurrentRole.Should().Be(UserRole.Worker);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var username = "admin";
        var password = "wrongpassword";

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeFalse();
        _authService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Logout_ClearsAuthenticationState()
    {
        // Arrange
        _authService.LoginAsync("admin", "password").Wait();
        _authService.IsAuthenticated.Should().BeTrue();

        // Act
        _authService.Logout();

        // Assert
        _authService.IsAuthenticated.Should().BeFalse();
        _authService.CurrentUsername.Should().BeNull();
        _authService.CurrentRole.Should().Be(UserRole.Customer);
    }

    [Fact]
    public void AuthenticationChanged_EventFired()
    {
        // Arrange
        var eventFired = false;
        _authService.AuthenticationChanged += () => eventFired = true;

        // Act
        _authService.Logout();

        // Assert
        eventFired.Should().BeTrue();
    }
}
```

#### Role-Based Access Tests

```csharp
public class RoleBasedAccessTests
{
    [Fact]
    public void Customer_CannotAccessWorkerFeatures()
    {
        // Arrange
        var authService = new AuthenticationService();
        authService.LoginAsync("user", "user123").Wait();

        // Assert
        authService.CurrentRole.Should().Be(UserRole.Customer);
        // Customer should not have access to scanner and analytics
    }

    [Fact]
    public void Worker_CanAccessWorkerFeatures()
    {
        // Arrange
        var authService = new AuthenticationService();
        authService.LoginAsync("worker", "worker123").Wait();

        // Assert
        authService.CurrentRole.Should().Be(UserRole.Worker);
        // Worker should have access to scanner and analytics
    }
}
```

### Integration Tests

#### End-to-End Authentication Flow

```csharp
public class AuthenticationFlowTests
{
    [Fact]
    public async Task CompleteLoginFlow_ShouldWork()
    {
        // Arrange
        var authService = new AuthenticationService();
        var loginPage = new LoginPage(authService);

        // Act
        var loginResult = await authService.LoginAsync("admin", "password");

        // Assert
        loginResult.Success.Should().BeTrue();
        authService.IsAuthenticated.Should().BeTrue();
        
        // Test UI updates
        var mainLayout = new MainLayout(authService);
        mainLayout.ShouldShowUserHeader();
        
        // Test role-based access
        var mainPage = new MainPage(null, authService);
        mainPage.ShouldShowWorkerButtons();
    }
}
```

---

## Frontend Integration

### JavaScript Bridge

#### Authentication State Management

```javascript
// wwwroot/js/auth-bridge.js
window.skiRentalAuth = {
    // Get current authentication state
    getAuthState: function() {
        return DotNet.invokeMethodAsync('AuthenticationService', 'GetAuthState');
    },
    
    // Login user
    login: function(username, password, rememberMe) {
        return DotNet.invokeMethodAsync('AuthenticationService', 'LoginAsync', 
            [username, password, rememberMe]);
    },
    
    // Logout user
    logout: function() {
        return DotNet.invokeMethodAsync('AuthenticationService', 'Logout');
    },
    
    // Subscribe to authentication changes
    subscribe: function(dotNetReference) {
        this.dotNetRef = dotNetReference;
    },
    
    // Handle authentication change from .NET
    onAuthChanged: function(authState) {
        // Update UI based on authentication state
        this.updateUI(authState);
    },
    
    updateUI: function(authState) {
        if (authState.isAuthenticated) {
            this.showAuthenticatedUI(authState);
        } else {
            this.showUnauthenticatedUI();
        }
    },
    
    showAuthenticatedUI: function(authState) {
        // Show user info, hide login button
        document.querySelector('.login-btn').style.display = 'none';
        document.querySelector('.user-info').style.display = 'block';
        document.querySelector('.user-name').textContent = authState.username;
        document.querySelector('.user-role').textContent = authState.role === 1 ? 'Pracownik' : 'Klient';
        
        // Show/hide role-based elements
        if (authState.role === 1) { // Worker
            document.querySelectorAll('.worker-only').forEach(el => {
                el.style.display = 'block';
            });
        } else {
            document.querySelectorAll('.worker-only').forEach(el => {
                el.style.display = 'none';
            });
        }
    },
    
    showUnauthenticatedUI: function() {
        // Show login button, hide user info
        document.querySelector('.login-btn').style.display = 'block';
        document.querySelector('.user-info').style.display = 'none';
        document.querySelectorAll('.worker-only').forEach(el => {
            el.style.display = 'none';
        });
    }
};
```

---

## Security Best Practices

### Production Recommendations

#### Password Security

```csharp
// Use proper password hashing
public class PasswordService
{
    private const int SaltLength = 16;
    private const int HashIterations = 10000;
    
    public string HashPassword(string password)
    {
        var salt = GenerateSalt();
        var hash = PBKDF2(password, salt, HashIterations);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }
    
    public bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var computedHash = PBKDF2(password, salt, HashIterations);
        return hash.SequenceEqual(computedHash);
    }
    
    private byte[] GenerateSalt()
    {
        var salt = new byte[SaltLength];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }
}
```

#### Session Security

```csharp
public class SessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    
    public string CreateSession(UserSession session)
    {
        var protector = _dataProtectionProvider.CreateProtector("SessionTokens");
        var sessionData = JsonSerializer.Serialize(session);
        var encryptedToken = protector.Protect(Encoding.UTF8.GetBytes(sessionData));
        
        // Set secure cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = session.ExpiresAt
        };
        
        _httpContextAccessor.HttpContext?.Response.Cookies.Append("SessionToken", 
            Convert.ToBase64String(encryptedToken), cookieOptions);
        
        return Convert.ToBase64String(encryptedToken);
    }
}
```

#### Rate Limiting

```csharp
public class RateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);
    
    public async Task<bool> IsRateLimitedAsync(string identifier)
    {
        var cacheKey = $"login_attempts_{identifier}";
        
        if (_cache.TryGetValue(cacheKey, out int attempts))
        {
            if (attempts >= 5)
            {
                return true; // Rate limited
            }
            
            _cache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(15));
        }
        else
        {
            _cache.Set(cacheKey, 1, TimeSpan.FromMinutes(15));
        }
        
        return false;
    }
}
```

### Audit Logging

```csharp
public class AuditService
{
    private readonly ILogger<AuditService> _logger;
    
    public void LogAuthenticationEvent(string username, bool success, string? ipAddress = null)
    {
        _logger.LogInformation("Authentication {Result} for user {Username} from {IpAddress}", 
            success ? "Success" : "Failure", username, ipAddress ?? "Unknown");
    }
    
    public void LogAuthorizationEvent(string username, string resource, bool success)
    {
        _logger.LogInformation("Authorization {Result} for user {Username} accessing {Resource}", 
            success ? "Granted" : "Denied", username, resource);
    }
    
    public void LogSecurityEvent(string eventType, string details, string? username = null)
    {
        _logger.LogWarning("Security Event {EventType}: {Details} (User: {Username})", 
            eventType, details, username ?? "Anonymous");
    }
}
```

---

## Monitoring and Analytics

### Authentication Metrics

#### Key Performance Indicators

| Metric | Description | Target |
|--------|-------------|--------|
| Login Success Rate | Percentage of successful login attempts | > 95% |
| Average Login Time | Time from request to successful login | < 2 seconds |
| Failed Login Rate | Percentage of failed login attempts | < 5% |
| Session Duration | Average session length per user | 30 minutes |
| Role Distribution | Percentage of users by role | Customer: 80%, Worker: 19%, Admin: 1% |

#### Monitoring Implementation

```csharp
public class AuthenticationMetrics
{
    private readonly IMetrics _metrics;
    
    public void RecordLoginAttempt(bool success, TimeSpan duration)
    {
        _metrics.Counter("login_attempts_total").Add(1, new[] { new KeyValuePair<string, object>("success", success) });
        _metrics.Histogram("login_duration_seconds").Record(duration.TotalSeconds);
    }
    
    public void RecordSessionStart(UserRole role)
    {
        _metrics.Counter("sessions_started_total").Add(1, new[] { new KeyValuePair<string, object>("role", role.ToString()) });
    }
    
    public void RecordSessionEnd(TimeSpan duration)
    {
        _metrics.Histogram("session_duration_seconds").Record(duration.TotalSeconds);
    }
}
```

---

## Troubleshooting

### Common Issues

#### Login Not Working

**Symptoms**: Login button doesn't respond or always shows failure

**Causes**:
1. Authentication service not properly injected
2. User credentials incorrect
3. Rate limiting active
4. JavaScript errors in browser

**Solutions**:
```csharp
// Check service injection
@inject SkiRental.App.Services.AuthenticationService AuthService

// Verify credentials (demo users)
// admin/password, worker/worker123, user/user123

// Check browser console for JavaScript errors
```

#### Role-Based UI Not Updating

**Symptoms**: Buttons not showing/hiding after login

**Causes**:
1. Event subscription missing
2. UI update not triggered
3. Role assignment incorrect

**Solutions**:
```csharp
// Ensure event subscription in MainPage constructor
_authService.AuthenticationChanged += OnAuthenticationChanged;

// Verify role assignment
Console.WriteLine($"Current role: {authService.CurrentRole}");

// Check UI update method
private void UpdateButtonVisibility()
{
    bool isWorker = _authService.IsAuthenticated && _authService.CurrentRole == UserRole.Worker;
    ScannerButton.IsVisible = isWorker;
    AnalyticsButton.IsVisible = isWorker;
}
```

#### Session Persistence Issues

**Symptoms**: User logged out after page refresh

**Causes**:
1. Session not properly stored
2. Session expiration too short
3. Browser privacy settings

**Solutions**:
```csharp
// Check session persistence
if (loginModel.RememberMe)
{
    // Store session in secure storage
    await StoreSessionAsync(sessionToken);
}

// Verify session expiration
if (_sessionExpiry < DateTime.UtcNow)
{
    // Session expired, logout user
    Logout();
}
```

---

## Future Enhancements

### Planned Features

#### Two-Factor Authentication

```csharp
public class TwoFactorAuthService
{
    public async Task<string> Generate2FACodeAsync(string userId)
    {
        // Generate 6-digit code
        var code = Random.Shared.Next(100000, 999999).ToString();
        
        // Send via SMS or email
        await Send2FACodeAsync(userId, code);
        
        // Store hashed code
        await Store2FACodeAsync(userId, Hash2FACode(code));
        
        return code;
    }
    
    public async Task<bool> Verify2FACodeAsync(string userId, string code)
    {
        var storedHash = await GetStored2FACodeAsync(userId);
        return Verify2FACode(code, storedHash);
    }
}
```

#### External Authentication Providers

```csharp
public class ExternalAuthService
{
    public async Task<LoginResult> LoginWithGoogleAsync(string token)
    {
        // Verify Google token
        var userInfo = await VerifyGoogleTokenAsync(token);
        
        // Create or update user account
        var user = await CreateOrUpdateUserAsync(userInfo);
        
        // Authenticate user
        return await LoginUserAsync(user);
    }
    
    public async Task<LoginResult> LoginWithFacebookAsync(string token)
    {
        // Similar implementation for Facebook
    }
}
```

#### Advanced Session Management

```csharp
public class AdvancedSessionService
{
    public async Task<SessionToken> RefreshSessionAsync(string refreshToken)
    {
        // Validate refresh token
        // Generate new access token
        // Update session
        // Return new tokens
    }
    
    public async Task RevokeAllSessionsAsync(string userId)
    {
        // Invalidate all user sessions
        // Force logout on all devices
    }
    
    public async Task<List<ActiveSession>> GetActiveSessionsAsync(string userId)
    {
        // Return list of active sessions
        // Include device info, location, last activity
    }
}
```

---

## Compliance and Regulations

### GDPR Compliance

#### Data Protection

```csharp
public class GDPRComplianceService
{
    public async Task<UserDataExport> ExportUserDataAsync(string userId)
    {
        // Collect all user data
        var userData = new UserDataExport
        {
            Profile = await GetUserProfileAsync(userId),
            Rentals = await GetUserRentalsAsync(userId),
            Payments = await GetUserPaymentsAsync(userId),
            ActivityLog = await GetUserActivityLogAsync(userId)
        };
        
        return userData;
    }
    
    public async Task DeleteUserDataAsync(string userId)
    {
        // Anonymize user data instead of deletion for audit purposes
        await AnonymizeUserDataAsync(userId);
        
        // Delete sensitive data
        await DeleteSensitiveUserDataAsync(userId);
    }
}
```

#### Consent Management

```csharp
public class ConsentService
{
    public async Task<bool> HasConsentAsync(string userId, ConsentType consentType)
    {
        // Check if user has given consent
    }
    
    public async Task RecordConsentAsync(string userId, ConsentType consentType, bool granted)
    {
        // Record consent with timestamp and IP address
    }
    
    public async Task WithdrawConsentAsync(string userId, ConsentType consentType)
    {
        // Handle consent withdrawal
    }
}
```

---

## Documentation and Training

### Developer Documentation

#### Authentication Integration Guide

```markdown
# Authentication Integration Guide

## Quick Start

1. Inject AuthenticationService
2. Subscribe to AuthenticationChanged event
3. Update UI based on authentication state
4. Implement role-based access control

## Code Examples

### Basic Usage
```csharp
@inject AuthenticationService Auth

@if (Auth.IsAuthenticated)
{
    <div>Welcome @Auth.CurrentUsername!</div>
}
else
{
    <a href="/login">Login</a>
}
```

### Role-Based Access
```csharp
@if (Auth.CurrentRole == UserRole.Worker)
{
    <WorkerOnlyComponent />
}
```
```

### User Training Materials

#### End User Guide

```markdown
# User Guide: Logging In and Account Management

## How to Log In

1. Click the login button (🔑) in the top right corner
2. Enter your username and password
3. Click "Zaloguj się"

## Demo Accounts

- **Customer**: user / user123
- **Worker**: worker / worker123
- **Admin**: admin / password

## Account Features

### For Customers
- Create and manage bookings
- View rental history
- Update profile information

### For Workers
- All customer features
- QR code scanner for returns
- Analytics dashboard
- Equipment management
```

---

This comprehensive authentication and authorization system documentation provides complete coverage of the security implementation in the SkiRental application. The system is designed to be secure, user-friendly, and easily extensible for future enhancements.
