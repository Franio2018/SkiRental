# SkiRental.App - MAUI Application Documentation

## Overview

The `SkiRental.App` project is a .NET MAUI hybrid application that combines native mobile capabilities with Blazor web UI. It provides cross-platform support for Android, iOS, Windows, and macOS Catalyst, featuring QR code scanning, NFC integration, and offline-first architecture.

## Project Structure

```
SkiRental.App/
├── MauiProgram.cs                 # Application entry point and DI setup
├── App.xaml / App.xaml.cs         # MAUI application configuration
├── MainPage.xaml / MainPage.xaml.cs # Main page with BlazorWebView and scanner
├── Routes.razor                   # Blazor routing configuration
├── _Imports.razor                 # Global Blazor imports
├── appsettings.json               # Application configuration
├── Pages/
│   ├── Home.razor                 # Main dashboard page
│   ├── BookingPage.razor          # Booking workflow wrapper
│   ├── EmployeePage.razor         # Employee interface wrapper
│   ├── AnalyticsPage.razor        # Analytics dashboard wrapper
│   ├── LoginPage.razor            # User authentication
│   ├── ProfilePage.razor          # User profile management
│   └── NotificationsPage.razor    # Notification center
├── Shared/
│   └── MainLayout.razor           # Shared layout component
├── Services/
│   ├── EfRepositories.cs          # Entity Framework repository implementations
│   ├── QrNfcBridgeService.cs      # MAUI-Blazor bridge for QR/NFC
│   ├── WeatherService.cs          # Weather data integration
│   ├── ProfilePictureService.cs   # Profile image management
│   └── AuthenticationService.cs     # User authentication and authorization
├── Platforms/                     # Platform-specific code
├── Resources/                      # App resources (images, fonts, etc.)
└── wwwroot/
    ├── index.html                  # Blazor host page
    └── css/
        ├── app.css                 # Main application styles
        ├── employee.css            # Employee-specific styles
        └── analytics.css           # Analytics-specific styles
```

## Application Architecture

### Hybrid Architecture

The application uses a hybrid MAUI-Blazor architecture:

```
┌─────────────────────────────────────────────────────────┐
│                   MAUI Native Layer                      │
│  ┌─────────────────────────────────────────────────┐    │
│  │  MainPage (XAML) + ZXing Camera + NFC Plugin    │    │
│  └─────────────────────────────────────────────────┘    │
└──────────────┬──────────────────────────────────────────┘
               │ JS Interop Bridge
┌──────────────▼──────────────────────────────────────────┐
│                   BlazorWebView                         │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Blazor Components (Routes, Pages, Layouts)     │    │
│  │  ┌─ Home.razor ── BookingWizard.razor          │    │
│  │  └─ EmployeePage ── QrScannerPanel.razor      │    │
│  └─────────────────────────────────────────────────┘    │
└──────────────┬──────────────────────────────────────────┘
               │ Service Layer
┌──────────────▼──────────────────────────────────────────┐
│              Business Logic & Data Access                │
│  ┌─────────────────────────────────────────────────┐    │
│  │  EquipmentMathService | BookingService           │    │
│  │  PaymentService | AnalyticsService              │    │
│  │  EF Core DbContext | Repository Pattern         │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

---

## Application Entry Point

### MauiProgram.cs

**File**: `MauiProgram.cs`

The main application configuration and dependency injection setup.

#### Service Registration

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // MAUI Configuration
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()                    // ZXing QR scanner
            .ConfigureFonts(ConfigureFonts)
            .UseMauiBlazorWebView();               // Blazor integration

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        // Database Configuration
        ConfigureDatabase(builder);

        // Payment Configuration
        ConfigurePaymentServices(builder);

        // HTTP Clients
        ConfigureHttpClients(builder);

        // Repository Registration
        builder.Services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        builder.Services.AddScoped<IRentalRepository, EfRentalRepository>();

        // Business Services
        builder.Services.AddScoped<EquipmentMathService>();
        builder.Services.AddScoped<BookingService>();
        builder.Services.AddScoped<AnalyticsService>();
        builder.Services.AddScoped<IPaymentProvider, PayUPaymentProvider>();

        // MAUI-Specific Services
        builder.Services.AddSingleton<QrNfcBridgeService>();
        builder.Services.AddSingleton<WeatherService>();
        builder.Services.AddSingleton<ProfilePictureService>();
        builder.Services.AddSingleton<AuthenticationService>();

        // Page Registration
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<App>();

        var app = builder.Build();

        // Database Migration
        MigrateDatabase(app);

        return app;
    }
}
```

#### Database Configuration

```csharp
private static void ConfigureDatabase(MauiAppBuilder builder)
{
    var dbPath = Path.Combine(
        FileSystem.AppDataDirectory, 
        "SkiRental.db");

    builder.Services.AddDbContext<SkiRentalDbContext>(opts =>
        opts.UseSqlite($"Data Source={dbPath}")
            .EnableSensitiveDataLogging(false),
        ServiceLifetime.Scoped);
}

private static void MigrateDatabase(MauiApp app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Startup");
        logger?.LogError(ex, "Failed to migrate SQLite database on startup.");
    }
}
```

#### Payment Configuration

```csharp
private static void ConfigurePaymentServices(MauiAppBuilder builder)
{
    builder.Services.Configure<PayUOptions>(opts =>
    {
        // Production vs Sandbox configuration
        opts.PosId        = Environment.GetEnvironmentVariable("PAYU_POS_ID") ?? "YOUR_POS_ID";
        opts.SignatureKey  = Environment.GetEnvironmentVariable("PAYU_SIGNATURE_KEY") ?? "YOUR_SIGNATURE_KEY";
        opts.ClientId      = Environment.GetEnvironmentVariable("PAYU_CLIENT_ID") ?? "YOUR_CLIENT_ID";
        opts.ClientSecret  = Environment.GetEnvironmentVariable("PAYU_CLIENT_SECRET") ?? "YOUR_CLIENT_SECRET";
        opts.BaseUrl       = "https://secure.snd.payu.com"; // Sandbox
        opts.NotifyUrl     = "https://YOUR_API/api/payu/notify";
    });
}
```

#### Font Configuration

```csharp
private static void ConfigureFonts(FontsBuilder fonts)
{
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    fonts.AddFont("Montserrat-Bold.ttf", "MontserratBold");
}
```

---

## Main Application Interface

### MainPage.xaml

**File**: `MainPage.xaml`

The primary MAUI page that hosts the BlazorWebView and QR scanner overlay.

#### Layout Structure

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:blazor="clr-namespace:Microsoft.AspNetCore.Components.WebView.Maui;assembly=Microsoft.AspNetCore.Components.WebView.Maui"
             xmlns:zxing="clr-namespace:ZXing.Net.Maui.Controls;assembly=ZXing.Net.Maui.Controls"
             x:Class="SkiRental.App.MainPage">

    <Grid RowDefinitions="*, Auto">
        
        <!-- BlazorWebView - Main Content Area -->
        <blazor:BlazorWebView x:Name="BlazorWebView"
                              HostPage="wwwroot/index.html"
                              Grid.Row="0">
            <blazor:BlazorWebView.RootComponents>
                <blazor:RootComponent Selector="#app"
                                     ComponentType="{x:Type local:Routes}" />
            </blazor:BlazorWebView.RootComponents>
        </blazor:BlazorWebView>

        <!-- QR Scanner Overlay -->
        <Grid x:Name="ScannerOverlay"
              Grid.Row="0"
              Grid.RowSpan="2"
              IsVisible="False"
              BackgroundColor="#CC000000">
            
            <Grid RowDefinitions="Auto, *, Auto" Margin="20">
                <Label Grid.Row="0"
                       Text="Skanuj kod QR klienta"
                       TextColor="White"
                       FontSize="20"
                       FontAttributes="Bold"
                       HorizontalOptions="Center"
                       Margin="0,20,0,10"/>

                <Frame Grid.Row="1"
                       CornerRadius="12"
                       Padding="0"
                       IsClippedToBounds="True"
                       Margin="0,10">
                    <zxing:CameraBarcodeReaderView x:Name="BarcodeReader"
                                                    BarcodesDetected="OnBarcodesDetected"
                                                    Options="{Binding BarcodeOptions}"/>
                </Frame>

                <Button Grid.Row="2"
                        Text="✕ Anuluj skanowanie"
                        BackgroundColor="Transparent"
                        BorderColor="White"
                        BorderWidth="1"
                        TextColor="White"
                        Clicked="CloseScannerAsync"
                        Margin="0,10,0,20"/>
            </Grid>
        </Grid>

        <!-- Bottom Navigation -->
        <Grid Grid.Row="1"
              BackgroundColor="{DynamicResource NavBarBackground}"
              ColumnDefinitions="*,*,*,*"
              Padding="0,8">

            <Button Grid.Column="0" x:Name="HomeButton"
                    Text="🏠&#10;Start"
                    Command="{Binding NavigateHomeCommand}"
                    Style="{StaticResource NavButtonStyle}" />
            
            <Button Grid.Column="1" x:Name="BookingButton"
                    Text="🎿&#10;Wypożycz"
                    Command="{Binding NavigateBookingCommand}"
                    Style="{StaticResource NavButtonStyle}" />
            
            <Button Grid.Column="2" x:Name="ScannerButton"
                    Text="📷&#10;Skaner"
                    Clicked="OpenScannerAsync"
                    Style="{StaticResource NavButtonStyle}" />
            
            <Button Grid.Column="3" x:Name="AnalyticsButton"
                    Text="📊&#10;Analityka"
                    Command="{Binding NavigateAnalyticsCommand}"
                    Style="{StaticResource NavButtonStyle}" />
        </Grid>

    </Grid>
</ContentPage>
```

### MainPage.xaml.cs

**File**: `MainPage.xaml.cs`

Code-behind that bridges native MAUI functionality with Blazor components.

#### Core Functionality

```csharp
public partial class MainPage : ContentPage
{
    private readonly QrNfcBridgeService _bridge;
    private readonly AuthenticationService _authService;

    // ZXing Configuration
    public BarcodeReaderOptions BarcodeOptions { get; } = new()
    {
        Formats = BarcodeFormat.QrCode,
        AutoRotate = true,
        Multiple = false
    };

    public MainPage(QrNfcBridgeService bridge, AuthenticationService authService)
    {
        InitializeComponent();
        _bridge = bridge;
        _authService = authService;
        BindingContext = this;

        // Event Subscriptions
        _bridge.QrCodeDetected += OnQrCodeDetectedFromBridge;
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        
        // Initial UI State
        UpdateButtonVisibility();
    }

    // Navigation Commands
    public Command NavigateHomeCommand => new(async () => await NavigateToAsync("/"));
    public Command NavigateBookingCommand => new(async () => await NavigateToAsync("/booking"));
    public Command NavigateAnalyticsCommand => new(async () => await NavigateToWithPermissionAsync("/analytics"));
}
```

#### QR Scanner Integration

```csharp
private async void OpenScannerAsync(object? sender, EventArgs e)
{
    // Authentication Check
    if (!_authService.IsAuthenticated || _authService.CurrentRole != UserRole.Worker)
    {
        await DisplayAlert("Brak uprawnień",
            "Tylko pracownicy mogą używać skanera QR.", "OK");
        return;
    }

    // Camera Permission Check
    var status = await Permissions.RequestAsync<Permissions.Camera>();
    if (status != PermissionStatus.Granted)
    {
        await DisplayAlert("Brak uprawnień",
            "Aplikacja potrzebuje dostępu do kamery aby skanować kody QR.", "OK");
        return;
    }

    // Start Scanner
    BarcodeReader.IsDetecting = true;
    ScannerOverlay.IsVisible = true;
}

private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
{
    var first = e.Results?.FirstOrDefault();
    if (first is null) return;

    // Stop detection after first scan
    BarcodeReader.IsDetecting = false;

    await MainThread.InvokeOnMainThreadAsync(async () =>
    {
        ScannerOverlay.IsVisible = false;
        await InvokeBlazerQrCallback(first.Value);
    });
}

private async Task InvokeBlazerQrCallback(string qrCode)
{
    await BlazorWebView.TryDispatchAsync(async sp =>
    {
        var js = sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
        await js.InvokeVoidAsync("skiRentalBridge.dispatchQrCode", qrCode);
    });
}
```

#### Role-Based UI Control

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

## Blazor Routing and Pages

### Routes.razor

**File**: `Routes.razor`

Blazor routing configuration with assembly loading for shared components.

```razor
@using System.Reflection
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web

<Router AppAssembly="@typeof(Routes).Assembly"
        AdditionalAssemblies="@ExtraAssemblies">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" 
                   DefaultLayout="@typeof(SkiRental.App.Shared.MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Nie znaleziono strony | SkiRental</PageTitle>
        <LayoutView Layout="@typeof(SkiRental.App.Shared.MainLayout)">
            <div class="not-found" style="text-align: center; padding: 2rem;">
                <h1>404 – Strona nie istnieje</h1>
                <p>Przepraszamy, ale nie znaleźliśmy niczego pod tym adresem.</p>
                <a href="/">Wróć do strony głównej</a>
            </div>
        </LayoutView>
    </NotFound>
</Router>

@code {
    private static readonly Assembly[] ExtraAssemblies =
    [
        typeof(SkiRental.UI.Shared.Components.Booking.BookingWizard).Assembly
    ];
}
```

### Page Components

#### Home.razor

**File**: `Pages/Home.razor`

Main dashboard with weather widget and role-based action cards.

```razor
@page "/"
@using SkiRental.App.Services

@inject WeatherService Weather
@inject AuthenticationService Auth

<PageTitle>SkiRental – Strona główna</PageTitle>

<div class="home-page">
    <header class="app-hero">
        <div class="hero-content">
            <div class="hero-logo">🎿</div>
            <h1>SkiRental</h1>
            <p class="hero-subtitle">Wypożyczalnia Nart Biegowych</p>
        </div>
    </header>

    <!-- Weather Widget -->
    @if (_weather is not null)
    {
        <div class="weather-widget @(_weather.IsGoodForSkiing ? "good" : "warning")">
            <div class="weather-icon">
                @if (!string.IsNullOrEmpty(_weather.Icon))
                {
                    <img src="@_weather.Icon" alt="Pogoda" />
                }
            </div>
            <div class="weather-info">
                <strong>@_weather.Location</strong>
                <span>@_weather.TemperatureCelsius.ToString("F1")°C · @_weather.Description</span>
                <span class="ski-condition">@_weather.SkiConditionLabel</span>
            </div>
        </div>
    }

    <!-- Action Cards -->
    <div class="action-grid">
        <a href="/booking" class="action-card primary">
            <span class="action-icon">🎿</span>
            <h3>Nowe Wypożyczenie</h3>
            <p>Dobierz sprzęt z pomocą Inteligentnego Asystenta</p>
        </a>
        
        <a href="/my-rentals" class="action-card">
            <span class="action-icon">📋</span>
            <h3>Moje Wypożyczenia</h3>
            <p>Historia i aktywne rezerwacje</p>
        </a>
        
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
    </div>
</div>

@code {
    private SkiWeatherReport? _weather;

    protected override async Task OnInitializedAsync()
    {
        try 
        { 
            _weather = await Weather.GetSkiConditionsAsync(); 
        }
        catch 
        { 
            /* Weather widget is optional */ 
        }
    }
}
```

#### LoginPage.razor

**File**: `Pages/LoginPage.razor`

User authentication interface with comprehensive error handling.

```razor
@page "/login"
@using System.ComponentModel.DataAnnotations
@inject SkiRental.App.Services.AuthenticationService AuthService

<PageTitle>Logowanie | SkiRental</PageTitle>

<div class="login-container">
    <div class="login-card">
        <div class="login-header">
            <div class="login-logo">
                <h1>🎿</h1>
                <h2>SkiRental</h2>
            </div>
            <p class="login-subtitle">Zaloguj się do swojego konta</p>
        </div>

        <EditForm Model="@loginModel" OnValidSubmit="@HandleLogin" class="login-form">
            <DataAnnotationsValidator />
            <ValidationSummary class="error-summary" />
            
            @if (!string.IsNullOrEmpty(errorMessage))
            {
                <div class="error-message">
                    @errorMessage
                </div>
            }

            <div class="form-group">
                <label for="username">Nazwa użytkownika lub email</label>
                <InputText id="username" class="form-input" 
                           @bind-Value="loginModel.Username" 
                           placeholder="Wprowadź nazwę użytkownika lub email" />
                <ValidationMessage For="@(() => loginModel.Username)" />
            </div>

            <div class="form-group">
                <label for="password">Hasło</label>
                <div class="password-input-wrapper">
                    <InputText type="@passwordType" id="password" class="form-input" 
                               @bind-Value="loginModel.Password" 
                               placeholder="Wprowadź hasło" />
                    <button type="button" class="password-toggle" @onclick="TogglePasswordVisibility">
                        @passwordIcon
                    </button>
                </div>
                <ValidationMessage For="@(() => loginModel.Password)" />
            </div>

            <button type="submit" class="login-button" disabled="@isLoggingIn">
                @if (isLoggingIn)
                {
                    <span class="spinner"></span>
                    <span>Logowanie...</span>
                }
                else
                {
                    <span>Zaloguj się</span>
                }
            </button>
        </EditForm>

        <div class="login-footer">
            <p>Demo użytkownicy:</p>
            <ul class="demo-users">
                <li><strong>admin</strong> / <code>password</code> (Pracownik)</li>
                <li><strong>user</strong> / <code>user123</code> (Klient)</li>
            </ul>
        </div>
    </div>
</div>

@code {
    private LoginModel loginModel = new();
    private bool isLoggingIn = false;
    private string passwordType = "password";
    private string passwordIcon = "👁️";
    private string? errorMessage;

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

    private async Task HandleLogin()
    {
        isLoggingIn = true;
        errorMessage = null;
        
        try
        {
            var result = await AuthService.LoginAsync(loginModel.Username, loginModel.Password, loginModel.RememberMe);
            
            if (result.Success)
            {
                NavigationManager.NavigateTo("/");
            }
            else
            {
                errorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Wystąpił błąd podczas logowania: {ex.Message}";
        }
        finally
        {
            isLoggingIn = false;
        }
    }

    private void TogglePasswordVisibility()
    {
        if (passwordType == "password")
        {
            passwordType = "text";
            passwordIcon = "🙈";
        }
        else
        {
            passwordType = "password";
            passwordIcon = "👁️";
        }
    }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
}
```

---

## Shared Layout

### MainLayout.razor

**File**: `Shared/MainLayout.razor`

Central layout component with authentication-aware header and navigation.

```razor
@* SkiRental.App/Shared/MainLayout.razor *@
@inherits LayoutComponentBase
@inject NavigationManager Navigation
@inject SkiRental.App.Services.AuthenticationService Auth
@using SkiRental.App.Services

<div class="main-layout">
    <!-- Application Header -->
    <header class="app-header">
        <div class="header-inner">
            <a href="/" class="header-logo">
                <span class="logo-icon">🎿</span>
                <span class="logo-text">SkiRental</span>
            </a>
            
            <div class="header-actions">
                @if (Auth.IsAuthenticated)
                {
                    <div class="user-info">
                        <span class="user-name">@Auth.CurrentUsername</span>
                        <span class="user-role">@(Auth.CurrentRole == UserRole.Worker ? "Pracownik" : "Klient")</span>
                    </div>
                    
                    <button class="header-btn" type="button" title="Powiadomienia"
                            @onclick='() => Navigation.NavigateTo("/notifications")'>
                        🔔
                        @if (unreadNotifications > 0)
                        {
                            <span class="notification-badge">@unreadNotifications</span>
                        }
                    </button>
                    
                    <button class="header-btn" type="button" title="Profil"
                            @onclick='() => Navigation.NavigateTo("/profile")'>👤</button>
                    
                    <button class="header-btn logout-btn" type="button" title="Wyloguj"
                            @onclick="HandleLogout">🚪</button>
                }
                else
                {
                    <button class="header-btn login-btn" type="button" title="Zaloguj się"
                            @onclick='() => Navigation.NavigateTo("/login")'>🔑</button>
                }
            </div>
        </div>
    </header>

    <!-- Main Content Area -->
    <main class="app-main">
        @Body
    </main>
</div>

@code {
    private int unreadNotifications = 3; // Demo value

    private void HandleLogout()
    {
        Auth.Logout();
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
```

---

## Application Services

### AuthenticationService

**File**: `Services/AuthenticationService.cs`

Comprehensive authentication and authorization system with role-based access control.

#### Core Features

```csharp
public class AuthenticationService
{
    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string? CurrentUserId { get; private set; }
    public UserRole CurrentRole { get; private set; } = UserRole.Customer;

    public event Action? AuthenticationChanged;

    // Demo Users (In production, use database)
    private readonly Dictionary<string, (string Password, string UserId, string Username, UserRole Role)> _users = new()
    {
        { "admin", ("password", "1", "Administrator", UserRole.Worker) },
        { "worker", ("worker123", "2", "Pracownik", UserRole.Worker) },
        { "user", ("user123", "3", "Klient", UserRole.Customer) },
        { "test@example.com", ("test123", "4", "Test Email User", UserRole.Customer) }
    };
}
```

#### Authentication Methods

```csharp
public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
{
    try
    {
        await Task.Delay(1000); // Simulate processing time

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new LoginResult(false, "Nazwa użytkownika i hasła są wymagane");
        }

        if (_users.TryGetValue(username.ToLower(), out var user))
        {
            if (user.Password == password)
            {
                // Successful login
                IsAuthenticated = true;
                CurrentUsername = user.Username;
                CurrentUserId = user.UserId;
                CurrentRole = user.Role;

                // Notify UI components
                AuthenticationChanged?.Invoke();

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

    // Notify UI components
    AuthenticationChanged?.Invoke();
}
```

### QrNfcBridgeService

**File**: `Services/QrNfcBridgeService.cs`

Bridge service that connects MAUI native functionality with Blazor components.

#### QR Code Bridge

```csharp
public class QrNfcBridgeService
{
    public event Action<string>? QrCodeDetected;

    public async Task DispatchQrCodeAsync(string qrCode)
    {
        // Bridge QR code from MAUI to Blazor
        QrCodeDetected?.Invoke(qrCode);
    }

    public async Task<string?> ReadNfcTagAsync()
    {
        try
        {
            if (await IsNfcAvailableAsync())
            {
                // Read NFC tag data
                var nfcData = await ReadNfcDataAsync();
                return nfcData;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NFC read error: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> WriteRentalToNfcTagAsync(string rentalId)
    {
        try
        {
            if (await IsNfcAvailableAsync())
            {
                var tagData = Convert.ToBase64String(Encoding.UTF8.GetBytes(rentalId));
                await WriteNfcDataAsync(tagData);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NFC write error: {ex.Message}");
        }
        
        return false;
    }

    private async Task<bool> IsNfcAvailableAsync()
    {
        // Check NFC availability on current platform
        return await Task.FromResult(true); // Simplified for demo
    }

    private async Task<string?> ReadNfcDataAsync()
    {
        // Platform-specific NFC reading implementation
        return await Task.FromResult("DEMO-NFC-DATA");
    }

    private async Task WriteNfcDataAsync(string data)
    {
        // Platform-specific NFC writing implementation
        await Task.CompletedTask;
    }
}
```

### WeatherService

**File**: `Services/WeatherService.cs`

Weather data integration for ski condition assessments.

```csharp
public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private const string ApiKey = "YOUR_OPENWEATHER_API_KEY";

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SkiWeatherReport> GetSkiConditionsAsync()
    {
        try
        {
            // Default to popular ski location (Zakopane, Poland)
            var response = await _httpClient.GetAsync(
                $"weather?q=Zakopane,PL&appid={ApiKey}&units=metric&lang=pl");
            
            response.EnsureSuccessStatusCode();
            
            var weatherData = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>();
            
            return new SkiWeatherReport
            {
                Location = weatherData.Name,
                TemperatureCelsius = weatherData.Main.Temp,
                Description = weatherData.Weather[0].Description,
                Humidity = weatherData.Main.Humidity,
                WindSpeed = weatherData.Wind.Speed,
                Icon = GetWeatherIcon(weatherData.Weather[0].Icon),
                IsGoodForSkiing = IsGoodSkiingWeather(weatherData),
                SkiConditionLabel = GetSkiConditionLabel(weatherData)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather data");
            
            // Return fallback data
            return new SkiWeatherReport
            {
                Location = "Zakopane",
                TemperatureCelsius = -5,
                Description = "Pogoda zimna",
                IsGoodForSkiing = true,
                SkiConditionLabel = "Warunki dobre"
            };
        }
    }

    private bool IsGoodSkiingWeather(OpenWeatherResponse weather)
    {
        // Good skiing conditions: -10°C to 0°C, low wind, light snow
        return weather.Main.Temp >= -10 && weather.Main.Temp <= 0 &&
               weather.Wind.Speed < 10 &&
               weather.Weather[0].Main.Contains("snow", StringComparison.OrdinalIgnoreCase);
    }

    private string GetSkiConditionLabel(OpenWeatherResponse weather)
    {
        if (IsGoodSkiingWeather(weather))
            return "Warunki doskonałe";
        
        if (weather.Main.Temp > 0)
            return "Śnieg topnieje";
        
        if (weather.Main.Temp < -15)
            return "Bardzo zimno - ostrożnie";
        
        return "Warunki akceptowalne";
    }

    private string GetWeatherIcon(string iconCode)
    {
        return $"https://openweathermap.org/img/wn/{iconCode}@2x.png";
    }
}
```

---

## Data Access Implementation

### EfRepositories.cs

**File**: `Services/EfRepositories.cs`

Entity Framework repository implementations for data access.

#### Equipment Repository

```csharp
public class EfEquipmentRepository : IEquipmentRepository
{
    private readonly SkiRentalDbContext _context;
    private readonly ILogger<EfEquipmentRepository> _logger;

    public EfEquipmentRepository(SkiRentalDbContext context, ILogger<EfEquipmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Equipments
            .Include(e => e.RentalEquipments)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Equipments
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Size)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken ct = default)
    {
        return await _context.Equipments
            .Where(e => e.Status == EquipmentStatus.Available)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Size)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Equipment>> GetAvailableByStyleAsync(
        SkiStyle style, EquipmentCategory category, CancellationToken ct = default)
    {
        return await _context.Equipments
            .Where(e => e.Status == EquipmentStatus.Available &&
                       e.Category == category &&
                       (e.CompatibleStyle == style || e.CompatibleStyle == null))
            .OrderBy(e => Math.Abs(e.Size - GetOptimalSize(style, 175))) // Sort by proximity to optimal size
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Equipment> CreateAsync(Equipment equipment, CancellationToken ct = default)
    {
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync(ct);
        return equipment;
    }

    public async Task<Equipment> UpdateAsync(Equipment equipment, CancellationToken ct = default)
    {
        _context.Equipments.Update(equipment);
        await _context.SaveChangesAsync(ct);
        return equipment;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var equipment = await GetByIdAsync(id, ct);
        if (equipment != null)
        {
            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync(ct);
        }
    }

    private decimal GetOptimalSize(SkiStyle style, int height)
    {
        return style == SkiStyle.Classic ? height + 25 : height + 12.5m;
    }
}
```

#### Rental Repository

```csharp
public class EfRentalRepository : IRentalRepository
{
    private readonly SkiRentalDbContext _context;
    private readonly ILogger<EfRentalRepository> _logger;

    public EfRentalRepository(SkiRentalDbContext context, ILogger<EfRentalRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Rental?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Rentals
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Rentals
            .Where(r => r.CustomerProfileId == customerId)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderByDescending(r => r.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Rental>> GetActiveRentalsAsync(CancellationToken ct = default)
    {
        return await _context.Rentals
            .Where(r => r.Status == RentalStatus.Active)
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderBy(r => r.EndTime)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Rental>> GetOverdueRentalsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Rentals
            .Where(r => r.Status == RentalStatus.Active && r.EndTime < now)
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderBy(r => r.EndTime)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Rental> CreateAsync(Rental rental, CancellationToken ct = default)
    {
        _context.Rentals.Add(rental);
        await _context.SaveChangesAsync(ct);
        return rental;
    }

    public async Task<Rental> UpdateAsync(Rental rental, CancellationToken ct = default)
    {
        _context.Rentals.Update(rental);
        await _context.SaveChangesAsync(ct);
        return rental;
    }

    public async Task<IReadOnlyList<Rental>> GetRentalsByDateRangeAsync(
        DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await _context.Rentals
            .Where(r => r.StartTime >= start && r.StartTime <= end)
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderByDescending(r => r.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
```

---

## Styling and Theming

### CSS Architecture

**File**: `wwwroot/css/app.css`

Main application stylesheet with comprehensive theming system.

#### Design System

```css
/* Alpine Ski Theme - Color Palette */
:root {
    --primary-color: #2c3e50;
    --primary-light: #34495e;
    --primary-dark: #1a252f;
    
    --accent-color: #3498db;
    --accent-light: #5dade2;
    --accent-dark: #2874a6;
    
    --success-color: #27ae60;
    --warning-color: #f39c12;
    --danger-color: #e74c3c;
    
    --neutral-50: #ecf0f1;
    --neutral-100: #bdc3c7;
    --neutral-200: #95a5a6;
    --neutral-300: #7f8c8d;
    --neutral-400: #34495e;
    --neutral-500: #2c3e50;
    
    --surface-color: #ffffff;
    --background-color: #f8f9fa;
    
    --text-primary: #2c3e50;
    --text-secondary: #7f8c8d;
    --text-muted: #95a5a6;
    
    --border-radius: 8px;
    --border-radius-lg: 12px;
    --border-radius-xl: 16px;
    
    --shadow-sm: 0 2px 4px rgba(0, 0, 0, 0.1);
    --shadow-md: 0 4px 8px rgba(0, 0, 0, 0.12);
    --shadow-lg: 0 8px 16px rgba(0, 0, 0, 0.15);
    --shadow-xl: 0 16px 32px rgba(0, 0, 0, 0.2);
    
    --transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
```

#### Layout Components

```css
/* Main Layout Structure */
.main-layout {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background-color: var(--background-color);
}

.app-header {
    background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-light) 100%);
    color: white;
    padding: 1rem 2rem;
    box-shadow: var(--shadow-md);
    position: sticky;
    top: 0;
    z-index: 1000;
}

.header-inner {
    display: flex;
    justify-content: space-between;
    align-items: center;
    max-width: 1200px;
    margin: 0 auto;
}

.header-logo {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    text-decoration: none;
    color: white;
    font-weight: bold;
    font-size: 1.25rem;
}

.logo-icon {
    font-size: 1.5rem;
}

.header-actions {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.user-info {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    margin-right: 1rem;
}

.user-name {
    font-weight: 600;
    font-size: 0.9rem;
}

.user-role {
    font-size: 0.8rem;
    opacity: 0.8;
}

.header-btn {
    background: rgba(255, 255, 255, 0.1);
    border: 1px solid rgba(255, 255, 255, 0.2);
    color: white;
    padding: 0.5rem;
    border-radius: var(--border-radius);
    cursor: pointer;
    transition: var(--transition);
    position: relative;
    font-size: 1.1rem;
}

.header-btn:hover {
    background: rgba(255, 255, 255, 0.2);
    transform: translateY(-1px);
}

.notification-badge {
    position: absolute;
    top: -4px;
    right: -4px;
    background: var(--danger-color);
    color: white;
    font-size: 0.7rem;
    padding: 2px 6px;
    border-radius: 10px;
    font-weight: bold;
}

.app-main {
    flex: 1;
    padding: 2rem;
    max-width: 1200px;
    margin: 0 auto;
    width: 100%;
}
```

#### Component Styles

```css
/* Home Page Styles */
.home-page {
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

.app-hero {
    background: linear-gradient(135deg, var(--accent-color) 0%, var(--accent-dark) 100%);
    color: white;
    padding: 4rem 2rem;
    border-radius: var(--border-radius-xl);
    text-align: center;
    box-shadow: var(--shadow-lg);
}

.hero-content {
    max-width: 600px;
    margin: 0 auto;
}

.hero-logo {
    font-size: 4rem;
    margin-bottom: 1rem;
}

.hero-content h1 {
    font-size: 3rem;
    font-weight: bold;
    margin-bottom: 0.5rem;
}

.hero-subtitle {
    font-size: 1.25rem;
    opacity: 0.9;
}

.weather-widget {
    background: var(--surface-color);
    border-radius: var(--border-radius-lg);
    padding: 1.5rem;
    box-shadow: var(--shadow-md);
    display: flex;
    align-items: center;
    gap: 1rem;
    border-left: 4px solid var(--accent-color);
}

.weather-widget.good {
    border-left-color: var(--success-color);
}

.weather-widget.warning {
    border-left-color: var(--warning-color);
}

.weather-icon img {
    width: 60px;
    height: 60px;
}

.weather-info {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.weather-info strong {
    font-size: 1.1rem;
    color: var(--text-primary);
}

.ski-condition {
    font-weight: 600;
    color: var(--accent-color);
}

.action-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1.5rem;
}

.action-card {
    background: var(--surface-color);
    border-radius: var(--border-radius-lg);
    padding: 2rem;
    text-decoration: none;
    color: var(--text-primary);
    box-shadow: var(--shadow-md);
    transition: var(--transition);
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    gap: 1rem;
}

.action-card:hover {
    transform: translateY(-4px);
    box-shadow: var(--shadow-xl);
}

.action-card.primary {
    background: linear-gradient(135deg, var(--accent-color) 0%, var(--accent-dark) 100%);
    color: white;
}

.action-card.employee {
    border-left: 4px solid var(--warning-color);
}

.action-card.analytics {
    border-left: 4px solid var(--success-color);
}

.action-icon {
    font-size: 3rem;
    opacity: 0.8;
}

.action-card h3 {
    font-size: 1.25rem;
    font-weight: 600;
    margin: 0;
}

.action-card p {
    opacity: 0.8;
    line-height: 1.5;
}
```

#### Login Page Styles

```css
/* Login Page Styles */
.login-container {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-light) 100%);
    padding: 2rem;
}

.login-card {
    background: var(--surface-color);
    border-radius: var(--border-radius-xl);
    padding: 3rem;
    box-shadow: var(--shadow-xl);
    width: 100%;
    max-width: 400px;
}

.login-header {
    text-align: center;
    margin-bottom: 2rem;
}

.login-logo h1 {
    font-size: 3rem;
    margin-bottom: 0.5rem;
}

.login-logo h2 {
    font-size: 1.5rem;
    font-weight: bold;
    color: var(--text-primary);
    margin-bottom: 0.5rem;
}

.login-subtitle {
    color: var(--text-secondary);
    margin-bottom: 0;
}

.login-form {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.form-group label {
    font-weight: 600;
    color: var(--text-primary);
}

.form-input {
    padding: 0.75rem;
    border: 2px solid var(--neutral-200);
    border-radius: var(--border-radius);
    font-size: 1rem;
    transition: var(--transition);
}

.form-input:focus {
    outline: none;
    border-color: var(--accent-color);
    box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.1);
}

.password-input-wrapper {
    position: relative;
}

.password-input-wrapper .form-input {
    padding-right: 3rem;
}

.password-toggle {
    position: absolute;
    right: 0.75rem;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    cursor: pointer;
    font-size: 1.2rem;
    color: var(--text-secondary);
}

.login-button {
    background: linear-gradient(135deg, var(--accent-color) 0%, var(--accent-dark) 100%);
    color: white;
    border: none;
    padding: 1rem;
    border-radius: var(--border-radius);
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    transition: var(--transition);
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
}

.login-button:hover:not(:disabled) {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
}

.login-button:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}

.spinner {
    width: 16px;
    height: 16px;
    border: 2px solid rgba(255, 255, 255, 0.3);
    border-top: 2px solid white;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.error-message {
    background: rgba(231, 76, 60, 0.1);
    color: var(--danger-color);
    padding: 1rem;
    border-radius: var(--border-radius);
    border-left: 4px solid var(--danger-color);
    font-size: 0.9rem;
}

.demo-users {
    list-style: none;
    padding: 0;
    margin: 1rem 0 0 0;
    font-size: 0.9rem;
    color: var(--text-secondary);
}

.demo-users li {
    margin-bottom: 0.5rem;
}

.demo-users code {
    background: var(--neutral-100);
    padding: 2px 6px;
    border-radius: 4px;
    font-family: monospace;
}
```

---

## Platform-Specific Code

### Android Platform

**File**: `Platforms/Android/MainApplication.cs`

```csharp
using Android.App;
using Android.Runtime;

[assembly: Application(Label = "SkiRental", Icon = "@mipmap/appicon", Theme = "@style/MainTheme", MainLauncher = true)]

namespace SkiRental.App.Platforms.Android
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
```

### iOS Platform

**File**: `Platforms/iOS/Program.cs`

```csharp
using UIKit;

namespace SkiRental.App.Platforms.iOS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
```

---

## Configuration Management

### appsettings.json

**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SkiRental.db"
  },
  "PayU": {
    "PosId": "YOUR_POS_ID",
    "SignatureKey": "YOUR_SIGNATURE_KEY",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "BaseUrl": "https://secure.snd.payu.com",
    "NotifyUrl": "https://your-api.com/api/payu/notify"
  },
  "OpenWeatherMap": {
    "ApiKey": "YOUR_OPENWEATHER_API_KEY",
    "DefaultLocation": "Zakopane,PL"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Features": {
    "EnableNfc": true,
    "EnableWeatherWidget": true,
    "EnablePushNotifications": false,
    "EnableOfflineMode": true
  }
}
```

---

## Performance Optimization

### Startup Performance

```csharp
// MauiProgram.cs - Optimized startup
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    // Configure fonts first (critical for UI)
    builder.ConfigureFonts(ConfigureFonts);

    // Then configure MAUI features
    builder
        .UseMauiApp<App>()
        .UseBarcodeReader()
        .UseMauiBlazorWebView();

    // Configure services in dependency order
    ConfigureCoreServices(builder);
    ConfigureBusinessServices(builder);
    ConfigureUiServices(builder);

    var app = builder.Build();

    // Run database migration in background
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("DatabaseMigration");
            logger?.LogError(ex, "Background database migration failed");
        }
    });

    return app;
}
```

### Memory Management

```csharp
// Dispose pattern for services
public class QrNfcBridgeService : IDisposable
{
    private Timer? _cleanupTimer;
    private bool _disposed = false;

    public QrNfcBridgeService()
    {
        // Periodic cleanup
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private void Cleanup(object? state)
    {
        // Cleanup expired QR codes and NFC data
        GC.Collect();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            QrCodeDetected = null;
            _disposed = true;
        }
    }
}
```

---

## Security Considerations

### Data Protection

```csharp
// Secure configuration handling
private static void ConfigurePaymentServices(MauiAppBuilder builder)
{
    var config = builder.Configuration;
    
    builder.Services.Configure<PayUOptions>(opts =>
    {
        // Use environment variables or secure storage
        opts.PosId = Environment.GetEnvironmentVariable("PAYU_POS_ID") 
                    ?? config["PayU:PosId"];
        opts.SignatureKey = Environment.GetEnvironmentVariable("PAYU_SIGNATURE_KEY") 
                           ?? config["PayU:SignatureKey"];
        // ... other sensitive configuration
    });
}
```

### Input Validation

```csharp
// Enhanced validation in login
public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
{
    // Input sanitization
    username = username?.Trim() ?? string.Empty;
    password = password?.Trim() ?? string.Empty;

    // Rate limiting (simplified)
    var attempts = GetLoginAttempts(username);
    if (attempts > 5)
    {
        return new LoginResult(false, "Zbyt wiele prób logowania. Spróbuj ponownie za 15 minut.");
    }

    try
    {
        // ... login logic
    }
    finally
    {
        // Track attempts
        TrackLoginAttempt(username);
    }
}
```

---

## Testing Strategy

### Unit Tests

```csharp
// Example: AuthenticationService Tests
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
        Assert.True(result.Success);
        Assert.True(_authService.IsAuthenticated);
        Assert.Equal("Administrator", _authService.CurrentUsername);
        Assert.Equal(UserRole.Worker, _authService.CurrentRole);
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
        Assert.False(result.Success);
        Assert.False(_authService.IsAuthenticated);
    }

    [Fact]
    public void Logout_ClearsAuthenticationState()
    {
        // Arrange
        _authService.LoginAsync("admin", "password").Wait();

        // Act
        _authService.Logout();

        // Assert
        Assert.False(_authService.IsAuthenticated);
        Assert.Null(_authService.CurrentUsername);
        Assert.Equal(UserRole.Customer, _authService.CurrentRole);
    }
}
```

---

## Deployment and Distribution

### Build Configuration

```xml
<!-- SkiRental.App.csproj -->
<PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>SkiRental.App</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Application Identity -->
    <ApplicationTitle>SkiRental</ApplicationTitle>
    <ApplicationId>pl.skirent.app</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>

    <!-- Platform-specific settings -->
    <AndroidMinSdkVersion>24</AndroidMinSdkVersion>
    <AndroidTargetSdkVersion>34</AndroidTargetSdkVersion>
    <iOSMinimumOSVersion>15.0</iOSMinimumOSVersion>
    <WindowsPackageType>None</WindowsPackageType>
</PropertyGroup>
```

### Environment Configuration

```bash
# Development
dotnet build -c Debug

# Release
dotnet build -c Release

# Android APK
dotnet publish -c Release -f net9.0-android

# iOS Bundle
dotnet publish -c Release -f net9.0-ios

# Windows MSIX
dotnet publish -c Release -f net9.0-windows10.0.19041.0
```

---

## Future Enhancements

### Planned Features

1. **Offline Mode**: Enhanced offline capabilities with data synchronization
2. **Push Notifications**: Real-time alerts for rental status changes
3. **Voice Commands**: Hands-free operation for staff
4. **Augmented Reality**: AR equipment visualization
5. **Multi-language Support**: Internationalization and localization

### Technology Roadmap

- **v2.1**: Enhanced offline mode and sync
- **v2.2**: Push notifications integration
- **v2.3**: Voice commands and accessibility improvements
- **v2.4**: AR equipment preview
- **v3.0**: Multi-tenant support and advanced analytics

---

## Version History

- **v1.0**: Initial MAUI application with basic functionality
- **v1.1**: Added QR scanner and authentication system
- **v1.2**: Enhanced UI with role-based access control
- **v1.3**: Weather integration and performance optimizations
- **v1.4**: Comprehensive styling and theming system
- **v1.5**: Enhanced security and error handling
