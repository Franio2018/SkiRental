# SkiRental.App — MAUI Hybrid Client

## Overview

**SkiRental.App** is the main executable: a **.NET MAUI** application hosting **Blazor WebView** UI. It targets cross-country ski rental workflows for **customers**, **workers**, and **owners**, with native **QR scanning**, **NFC**, local **SQLite**, and integrations (PayU, OpenWeather).

| Property | Value |
|----------|-------|
| SDK | MAUI + Blazor WebView |
| Target frameworks | `net9.0-android`, `ios`, `maccatalyst`, `windows10.0.19041.0` |
| ApplicationId | `pl.skirent.app` |
| Config | `appsettings.json` as **EmbeddedResource** |

---

## Project structure

```
SkiRental.App/
├── MauiProgram.cs              # DI composition root
├── App.xaml / App.xaml.cs
├── MainPage.xaml / MainPage.xaml.cs   # BlazorWebView + ZXing overlay
├── Routes.razor                # Router + UI.Shared assembly
├── _Imports.razor
├── appsettings.json
├── Pages/
│   ├── Public/           Home, NotificationsPage, NoMatchPage
│   ├── Auth/             LoginPage, RegisterPage
│   ├── Booking/          BookingPage, CalendarBookingPage, RecommendationLoadingPage,
│   │                     GearReviewPage, PaymentPage, ConfirmationPage
│   ├── Profile/          ProfilePage, MyRentalsPage, EmployeePage
│   └── Management/       ManageOrdersPage, ManageCustomersPage, AnalyticsPage,
│                         EquipmentManagementPage, StaffManagementPage
├── Shared/
│   ├── MainLayout.razor
│   ├── AuthorizeRouteView.razor    # Role-based route guard
│   └── ToastContainer.razor
├── Services/
│   ├── AuthenticationService.cs
│   ├── JwtTokenService.cs, JwtOptions.cs
│   ├── BookingStateService.cs
│   ├── QrNfcBridgeService.cs
│   ├── WeatherService.cs
│   ├── ToastService.cs
│   ├── ProfilePictureService.cs
│   └── EfRepositories.cs           # Duplicate of Logic repos — NOT registered in DI
├── Platforms/              Android, iOS, MacCatalyst, Windows
├── Resources/              Fonts, images, MAUI styles
└── wwwroot/
    ├── index.html          # skiRentalBridge JavaScript
    └── css/                app.css, employee.css, analytics.css, new-rental-flow.css
```

---

## Architecture within the solution

```
┌─────────────────────────────────────────────┐
│ MainPage (MAUI XAML)                        │
│  • BlazorWebView → Routes.razor             │
│  • ZXing CameraBarcodeReaderView (overlay)  │
└──────────────────┬──────────────────────────┘
                   │ IJSRuntime / skiRentalBridge
┌──────────────────▼──────────────────────────┐
│ Blazor Pages + SkiRental.UI.Shared          │
│  AuthorizeRouteView → MainLayout → @Body    │
└──────────────────┬──────────────────────────┘
                   │ DI
┌──────────────────▼──────────────────────────┐
│ App Services (singleton/scoped)             │
│  + SkiRental.Logic (AddSkiRentalServices)   │
└──────────────────┬──────────────────────────┘
                   │
            SQLite: AppDataDirectory/SkiRental.db
```

---

## MauiProgram.cs — startup and DI

### MAUI and Blazor

```csharp
builder.UseMauiApp<App>()
       .UseBarcodeReader()           // ZXing.Net.Maui
       .UseMauiBlazorWebView();
```

### Configuration load

Embedded resource stream: `SkiRental.App.appsettings.json` → `builder.Configuration.AddJsonStream(stream)`.

### Database

```csharp
var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "SkiRental.db");
builder.Services.AddSkiRentalServices($"Data Source={localDbPath}", configuration: config);
```

On startup (after build):

- `db.Database.Migrate()`
- Deletes legacy `Gloves` / `Helmet` equipment rows (shop no longer rents these)
- Writes `startup.log` to app data for diagnostics

### JWT (`JwtOptions`)

Section `Jwt`: `SecretKey`, `Issuer`, `Audience`, `ExpirationMinutes`, `ClockSkewMinutes`. Dev fallback key if secret too short.

### PayU

`Configure<PayUOptions>(config.GetSection("PayU"))`  
`IPaymentProvider` registered **scoped** with `IHttpClientFactory` client named `PayUPaymentProvider`.

### Weather

Singleton `WeatherService` using named client `WeatherClient` → `https://api.openweathermap.org/`.

### App-layer service lifetimes

| Service | Lifetime | Role |
|---------|----------|------|
| `JwtTokenService` | Singleton | Create/validate JWT |
| `AuthenticationService` | Singleton | Session, login, implements `IAuthenticationService` |
| `QrNfcBridgeService` | Singleton | QR events, NFC read/write |
| `ProfilePictureService` | Singleton | Local profile image files |
| `MainPage`, `App` | Singleton | MAUI shell |
| `BookingStateService` | Scoped | Wizard state between booking pages |
| `ToastService` | Scoped | UI toast messages |

### Session restore

After `Build()`, calls `AuthenticationService.TryRestoreSessionAsync()` for “remember me” JWT in secure storage.

---

## Routing and authorization

### Routes.razor

Uses `AuthorizeRouteView` instead of default `RouteView` for protected pages.

Loads **SkiRental.UI.Shared** as additional assembly for `BookingWizard`, `QrScannerPanel`, etc.

### AuthorizeRouteView policies

| Paths | Auth | Minimum role |
|-------|------|----------------|
| `/`, `/login`, `/register` | Public | — |
| `/booking`, `/calendar-booking`, `/recommendation-loading`, `/gear-review`, `/payment`, `/confirmation`, `/no-match`, `/my-rentals`, `/profile`, `/notifications` | Required | Any authenticated |
| `/employee`, `/manage-orders`, `/manage-customers`, `/analytics` | Required | **Worker** (Owner included) |
| `/equipment-management`, `/staff-management` | Required | **Owner** |

Unauthenticated users → `/login?returnUrl=...`  
Wrong role → `/`

`AuthenticationService` exposes `IsOwner`, `IsWorker`, `CurrentRole` (`AppRole`).

---

## Page map

| Route | File | Audience | Function |
|-------|------|----------|----------|
| `/` | `Public/Home.razor` | All | Dashboard, weather, quick links |
| `/login` | `Auth/LoginPage.razor` | Public | Login |
| `/register` | `Auth/RegisterPage.razor` | Public | Registration |
| `/notifications` | `Public/NotificationsPage.razor` | Auth | In-app inbox |
| `/calendar-booking` | `Booking/CalendarBookingPage.razor` | Auth | Pick dates |
| `/recommendation-loading` | `Booking/RecommendationLoadingPage.razor` | Auth | Run orchestrator |
| `/gear-review` | `Booking/GearReviewPage.razor` | Auth | Review/swap gear |
| `/no-match` | `Public/NoMatchPage.razor` | Auth | Inventory gap explanation |
| `/payment` | `Booking/PaymentPage.razor` | Auth | Spot / BLIK |
| `/confirmation` | `Booking/ConfirmationPage.razor` | Auth | Receipt |
| `/booking` | `Booking/BookingPage.razor` | Auth | Legacy `BookingWizard` host |
| `/my-rentals` | `Profile/MyRentalsPage.razor` | Auth | Customer history |
| `/profile` | `Profile/ProfilePage.razor` | Auth | Edit profile |
| `/employee` | `Profile/EmployeePage.razor` | Worker+ | Returns, scanner entry |
| `/manage-orders` | `Management/ManageOrdersPage.razor` | Worker+ | Active/pending orders |
| `/manage-customers` | `Management/ManageCustomersPage.razor` | Worker+ | Customer list |
| `/analytics` | `Management/AnalyticsPage.razor` | Worker+ | `UtilizationDashboard` |
| `/equipment-management` | `Management/EquipmentManagementPage.razor` | Owner | Fleet CRUD |
| `/staff-management` | `Management/StaffManagementPage.razor` | Owner | AppUser admin |

---

## Booking flow (page-based)

State holder: **`BookingStateService`** (scoped).

```
CalendarBookingPage
    → sets dates, loads profile parameters
RecommendationLoadingPage
    → IRecommendationOrchestrator.RecommendAsync
GearReviewPage
    → GearItemCard, SwapDrawer; or redirect to NoMatchPage
PaymentPage
    → PaymentMethodSelector, OnlinePaymentPanel
    → BookingService.CreateBookingAsync
ConfirmationPage
    → ReceiptSummary, QR display
```

Pickup at desk: worker activates rental (`ActivateRentalAsync`) so equipment moves from **Reserved** to **Rented**.

---

## MainPage — native bridge

**XAML:** `BlazorWebView` full screen; `ScannerOverlay` with `CameraBarcodeReaderView` (QR only).

**Code-behind:**

- Injects `QrNfcBridgeService`, `AuthenticationService`
- Worker-only scanner button visibility
- `OnBarcodesDetected` → hides overlay → `skiRentalBridge.dispatchQrCode` via `BlazorWebView.TryDispatchAsync`
- Camera permission via `Permissions.Camera`

**JS (`wwwroot/index.html`):**

```javascript
window.skiRentalBridge = {
  dispatchQrCode: function (code) {
    // invokes .NET handler registered by QrScannerPanel / EmployeePage
  }
};
```

---

## App services (detailed)

### AuthenticationService

- Implements `IAuthenticationService` for UI.Shared
- Uses `IServiceScopeFactory` to resolve scoped `UserService` / `SkiRentalDbContext` from singleton
- Login: BCrypt verify via `UserService`, issues JWT via `JwtTokenService`
- Demo seeded users: `owner`/`owner123`, `worker`/`worker123`, `user`/`user123`
- Events: `AuthenticationChanged` for layout refresh
- **Not** the same as deprecated in-memory-only demo dict in old docs — production path is database-backed

### BookingStateService

Properties typical: selected dates, `RecommendationResult`, chosen equipment ids, payment method, running total. Cleared after confirmation or cancel.

### QrNfcBridgeService

- `QrCodeDetected` event
- Plugin.NFC for tag read/write (rental id as payload)
- Platform availability checks

### WeatherService

- OpenWeather 2.5 API with lat/lon from config (default near Zakopane)
- Cache duration `OpenWeather:CacheMinutes`
- Returns DTO for home widget (temperature, description, ski condition label)

### ToastService

Scoped messages consumed by `ToastContainer.razor`.

### ProfilePictureService

Saves images under app data; stores path on `UserProfile.ProfileImagePath`.

### EfRepositories.cs (unused)

Duplicate repository implementations — **Logic `EfRepositories` are registered** via `AddSkiRentalServices`. Safe to delete in a refactor; documented to avoid confusion.

---

## Configuration reference (`appsettings.json`)

| Section | Purpose |
|---------|---------|
| `ConnectionStrings` | Sqlite/SqlServer templates (App uses app data path override) |
| `Jwt` | Token signing |
| `PayU` | Sandbox/production PayU + **NotifyUrl** → Api |
| `OpenWeather` | ApiKey, coordinates, cache |
| `Email` | SendGrid SMTP for `NotificationService` |
| `Sms` | Twilio |
| `SkiRental` | Season, max days, fees, support |
| `Logging` | Levels |

**Production:** Replace secrets; use environment variables or secure storage; never commit real PayU/OpenWeather keys.

---

## Platform folders

| Folder | Responsibility |
|--------|----------------|
| `Platforms/Android` | Manifest, MainActivity, NFC/permissions |
| `Platforms/iOS` | Info.plist, AppDelegate |
| `Platforms/MacCatalyst` | Desktop Mac hosting |
| `Platforms/Windows` | Package manifest, windowing |

Each implements MAUI entry points calling `MauiProgram.CreateMauiApp()`.

---

## Build and run

```bash
# Windows
dotnet build SkiRental.App/SkiRental.App.csproj -f net9.0-windows10.0.19041.0
dotnet run --project SkiRental.App -f net9.0-windows10.0.19041.0

# Android (requires SDK)
dotnet build -f net9.0-android
```

Visual Studio: select target platform, F5.

---

## Dependencies (csproj highlights)

| Package | Role |
|---------|------|
| `Microsoft.Maui.Controls` | MAUI shell |
| `Microsoft.AspNetCore.Components.WebView.Maui` | Blazor hybrid |
| `Microsoft.EntityFrameworkCore.Sqlite` | Local DB |
| `ZXing.Net.Maui.Controls` | QR |
| `Plugin.NFC` | NFC tags |
| `System.IdentityModel.Tokens.Jwt` | JWT |
| `BCrypt.Net-Next` | Used via Logic `UserService` |
| `OpenWeatherMap.Standard` or HttpClient | Weather |

**Project references:** `SkiRental.Domain`, `SkiRental.Logic`, `SkiRental.UI.Shared`

---

## Troubleshooting

| Issue | Check |
|-------|--------|
| Migration failed | `AppDataDirectory/startup.log` |
| PayU HttpClient null | Only one registration of `PayUPaymentProvider`; must use factory |
| Scanner not visible | User role Worker+, camera permission |
| Empty recommendations | Seed data / equipment status Available |
| Webhook not updating payment | Api running, NotifyUrl reachable, see [SkiRental.Api.md](SkiRental.Api.md) |

---

## See also

- [Architecture.md](Architecture.md)
- [Authentication-System.md](Authentication-System.md)
- [SkiRental.UI.Shared.md](SkiRental.UI.Shared.md)
- [SkiRental.Logic.md](SkiRental.Logic.md)
- [SkiRental.Api.md](SkiRental.Api.md)
