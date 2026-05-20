# 🎿 SkiRental – System Wynajmu Nart Biegowych

> **.NET 9 MAUI Hybrid (Blazor) · EF Core · PayU BLIK · ZXing QR/NFC · OpenWeather**

---

## 📋 Spis treści

1. [Architektura rozwiązania](#architektura)
2. [Struktura projektu](#struktura)
3. [Szybki start](#szybki-start)
4. [Algorytm "Inteligentny Asystent"](#algorytm)
5. [Modele danych (EF Core)](#modele)
6. [Integracja płatności (PayU BLIK)](#platnosci)
7. [Panel Pracownika – QR/NFC](#qr-nfc)
8. [Analityka – UtilizationRate](#analityka)
9. [Konfiguracja bazy danych](#baza-danych)
10. [Testy jednostkowe](#testy)
11. [Roadmap](#roadmap)

### 📚 English documentation (per project)

Detailed, up-to-date docs for each project live in **[docs/README.md](docs/README.md)**:

- [Architecture](docs/Architecture.md) · [Domain](docs/SkiRental.Domain.md) · [Logic](docs/SkiRental.Logic.md)
- [App](docs/SkiRental.App.md) · [UI.Shared](docs/SkiRental.UI.Shared.md) · [Api](docs/SkiRental.Api.md) · [Tests](docs/SkiRental.Tests.md)

---

## 🏗️ Architektura rozwiązania <a name="architektura"></a>

```
┌─────────────────────────────────────────────────────────┐
│                   SkiRental.App                         │
│          .NET MAUI (Android / iOS / Windows)            │
│                                                         │
│  ┌─────────────────────────────────────────────────┐    │
│  │         BlazorWebView (Hybrid)                  │    │
│  │   BookingWizard | QrScannerPanel | Dashboard    │    │
│  └───────────────────┬─────────────────────────────┘    │
│                      │ JS Interop                       │
│  ┌───────────────────▼─────────────────────────────┐    │
│  │  MAUI Native Layer                              │    │
│  │  ZXing BarcodeReader | NFC Plugin | Permissions │    │
│  └─────────────────────────────────────────────────┘    │
└──────────────┬──────────────────────────────────────────┘
               │ DI / Service References
┌──────────────▼──────────────────────────────────────────┐
│                  SkiRental.Logic                         │
│   EquipmentMathService | BookingService | Analytics      │
│   PayUPaymentProvider  | EF DbContext   | Repositories   │
└──────────────┬──────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────┐
│                  SkiRental.Domain                        │
│   Equipment | UserProfile | Rental | RentalEquipment     │
│   Enums: SkiStyle | SkillLevel | EquipmentStatus         │
└─────────────────────────────────────────────────────────┘

Zewnętrzne integracje:
  PayU REST API  ──►  InitiateBlikPayment / Capture / Refund
  OpenWeather API ──► Warunki narciarskie (temperatura, wiatr, śnieg)
  SQLite (offline / mobilny)  ◄──► SQL Server (back-office)
```

---

## 📁 Struktura projektu <a name="struktura"></a>

```
SkiRental/
├── SkiRental.sln
│
├── SkiRental.Domain/                  ← Warstwa domenowa (Models, Enums)
│   ├── Enums/
│   │   └── Enums.cs                   ← SkiStyle, SkillLevel, EquipmentStatus, ...
│   └── Models/
│       ├── Equipment.cs               ← Sprzęt (narty, kijki, buty)
│       ├── UserProfile.cs             ← Dane fizyczne klienta + BMI
│       ├── Rental.cs                  ← Wypożyczenie + RentalEquipment + Payment
│       └── EquipmentRecommendation.cs ← DTOs algorytmu + UtilizationReport
│
├── SkiRental.Logic/                   ← Warstwa logiki biznesowej
│   ├── Interfaces/
│   │   └── IInterfaces.cs             ← IPaymentProvider, IEquipmentRepository, IRentalRepository
│   ├── Services/
│   │   ├── EquipmentMathService.cs    ← 🤖 Algorytm "Inteligentny Asystent"
│   │   ├── BookingService.cs          ← Cykl życia wypożyczenia
│   │   ├── PaymentService.cs          ← PayU OAuth2 + BLIK + pre-auth
│   │   └── AnalyticsService.cs        ← UtilizationRate + KPI
│   └── SkiRentalDbContext.cs          ← EF Core DbContext (SQLite / SQL Server)
│
├── SkiRental.UI.Shared/               ← Współdzielone komponenty Blazor (RCL)
│   └── Components/
│       ├── Booking/
│       │   └── BookingWizard.razor    ← Kreator rezerwacji (3 kroki + BLIK)
│       ├── Employee/
│       │   └── QrScannerPanel.razor   ← Panel pracownika (QR + ocena stanu)
│       └── Analytics/
│           └── UtilizationDashboard.razor ← Back-office KPI
│
├── SkiRental.App/                     ← Projekt MAUI (entry point)
│   ├── MauiProgram.cs                 ← DI: EF, PayU, ZXing, WeatherService
│   ├── MainPage.xaml / .cs            ← BlazorWebView + ZXing overlay + nawigacja
│   ├── Routes.razor                   ← Router Blazor
│   ├── Pages/
│   │   ├── Home.razor                 ← Strona główna + widżet pogodowy
│   │   ├── BookingPage.razor          ← Opakowuje BookingWizard
│   │   ├── EmployeePage.razor         ← Opakowuje QrScannerPanel
│   │   └── AnalyticsPage.razor        ← Opakowuje UtilizationDashboard
│   ├── Shared/
│   │   └── MainLayout.razor           ← Layout z nagłówkiem
│   ├── Services/
│   │   ├── EfRepositories.cs          ← EfEquipmentRepository + EfRentalRepository
│   │   ├── QrNfcBridgeService.cs      ← MAUI ↔ Blazor bridge (ZXing + NFC)
│   │   └── WeatherService.cs          ← OpenWeather API (warunki narciarskie)
│   └── wwwroot/
│       ├── index.html                 ← Host page + skiRentalBridge JS
│       └── css/
│           ├── app.css                ← Design system (paleta alpejska)
│           ├── employee.css           ← Style panelu pracownika
│           └── analytics.css          ← Style dashboardu analitycznego
│
└── SkiRental.Tests/                   ← Testy jednostkowe (xUnit + Moq + FluentAssertions)
    └── EquipmentMathServiceTests.cs   ← 10+ testów algorytmu doboru sprzętu
```

---

## 🚀 Szybki start <a name="szybki-start"></a>

### Wymagania wstępne

| Narzędzie                | Wersja        |
|--------------------------|---------------|
| Visual Studio            | 2022 v17.10+  |
| .NET SDK                 | 9.0+          |
| MAUI Workload            | `dotnet workload install maui` |
| Android SDK              | API 34        |

### Klonowanie i uruchomienie

```bash
# 1. Klonuj repozytorium
git clone https://github.com/your-org/SkiRental.git
cd SkiRental

# 2. Zainstaluj workloads
dotnet workload install maui

# 3. Przywróć pakiety
dotnet restore

# 4. Utwórz bazę danych SQLite (pierwsze uruchomienie)
cd SkiRental.Logic
dotnet ef migrations add InitialCreate --startup-project ../SkiRental.App
dotnet ef database update --startup-project ../SkiRental.App

# 5. Uruchom aplikację (Android Emulator / Windows)
cd ../SkiRental.App
dotnet run -f net9.0-android   # Android
dotnet run -f net9.0-windows10.0.19041.0  # Windows
```

### Konfiguracja kluczy API

Otwórz `SkiRental.App/MauiProgram.cs` i uzupełnij:

```csharp
// PayU (Sandbox: https://secure.snd.payu.com)
opts.PosId        = "YOUR_POS_ID";
opts.ClientId      = "YOUR_CLIENT_ID";
opts.ClientSecret  = "YOUR_CLIENT_SECRET";
opts.NotifyUrl     = "https://YOUR_DOMAIN/api/payu/notify";

// OpenWeather (https://openweathermap.org/api)
// Otwórz: SkiRental.App/Services/WeatherService.cs
private const string ApiKey = "YOUR_OPENWEATHER_API_KEY";
```

> **Bezpieczeństwo**: W środowisku produkcyjnym użyj Azure Key Vault,
> Android Keystore lub iOS Keychain zamiast hardkodowania kluczy.

---

## 🤖 Algorytm "Inteligentny Asystent" <a name="algorytm"></a>

Implementacja: `SkiRental.Logic/Services/EquipmentMathService.cs`

### Wzory obliczeniowe

| Parametr          | Styl Klasyczny              | Styl Łyżwowy (Skate)        |
|-------------------|-----------------------------|-----------------------------|
| **Długość nart**  | wzrost + 20..30 cm *        | wzrost + 10..15 cm *        |
| **Kijki**         | wzrost × 0.85               | wzrost × 0.90               |
| **Korekta BMI**   | ±5 cm przy BMI poza 18.5–24.9 | ±5 cm przy BMI poza 18.5–24.9 |

*) Offset interpolowany liniowo względem poziomu zaawansowania:
   `Beginner → min offset`, `Expert → max offset`

### Flex Index (sztywność narty)

| Poziom             | Bazowy Flex | Korekta nadwaga | Korekta niedowaga |
|--------------------|-------------|-----------------|-------------------|
| Beginner           | 42          | +5 → 47         | −5 → 37           |
| Intermediate       | 52          | +5 → 57         | −5 → 47           |
| Advanced           | 62          | +5 → 67         | −5 → 57           |
| Expert             | 72          | +5 → 77         | −5 → 67           |

### Przykład użycia (C#)

```csharp
var params = new EquipmentSelectionParameters
{
    Height     = 175,  // cm
    Weight     = 78m,  // kg  → BMI = 25.5 → korekta nadwaga
    ShoeSize   = 43,
    Style      = SkiStyle.Classic,
    SkillLevel = SkillLevel.Intermediate
};

var recommendation = await equipmentMathService.RecommendAsync(params);

// recommendation.RecommendedSkiLength  = 202.5 cm  (175+22.5+5 korekta)
// recommendation.RecommendedPoleLength = 147.5 cm  (175×0.85)
// recommendation.RecommendedStiffness  = 57         (52+5 korekta)
// recommendation.BmiCorrectionApplied  = true
```

---

## 🗄️ Modele danych (EF Core) <a name="modele"></a>

### Equipment

```
Equipment
├── Id              (PK)
├── SerialNumber    (UNIQUE)
├── Brand / Model
├── Category        ← Skis | Poles | Boots | Gloves | Helmet
├── Size            ← długość [cm] lub rozmiar buta [EU]
├── Stiffness       ← Flex Index 1–120
├── Status          ← Available | Rented | Maintenance | Retired
├── CompatibleStyle ← Classic | Skate | Combi
├── DailyRate       ← PLN/dobę
└── TotalRentalDays ← do analityki
```

### UserProfile (PhysicalData)

```
UserProfile
├── Id
├── FirstName / LastName / Email / PhoneNumber
├── Height     [cm]      ← Range 100–250
├── Weight     [kg]      ← Range 30–300
├── ShoeSize   [EU]      ← Range 30–55
├── SkillLevel           ← Beginner | Intermediate | Advanced | Expert
├── PreferredStyle       ← Classic | Skate | Combi
├── Bmi        [notmapped] ← obliczany z Height/Weight
└── IsBmiNormal [notmapped]
```

### Rental (agregat główny)

```
Rental
├── Id
├── CustomerProfileId  (FK → UserProfile)
├── StartTime / EndTime / ActualReturnTime
├── QrCode             ← unikalny token do zwrotu (UNIQUE INDEX)
├── NfcTagData         ← Base64 payload tagu NFC
├── IsPreauthorized    ← czy PayU pre-auth wykonany
├── PaymentStatus      ← Unpaid | Preauthorized | Paid | Refunded | Failed
├── TotalAmount        ← PLN
├── PayUOrderId
├── Status             ← Pending | Active | Completed | Cancelled | Overdue
└── RentalEquipments   ← ICollection<RentalEquipment> (M:N z Equipment)

RentalEquipment (join table)
├── RentalId + EquipmentId  (PK composite)
├── ConditionAtPickup   ← 1–5 (ocena przy wydaniu)
└── ConditionAtReturn   ← 1–5 (ocena przy zwrocie)
```

### Migracje EF Core

```bash
# Dodaj migrację po zmianie modelu
dotnet ef migrations add NazwaMigracji \
  --project SkiRental.Logic \
  --startup-project SkiRental.App

# Zastosuj migrację
dotnet ef database update \
  --project SkiRental.Logic \
  --startup-project SkiRental.App
```

---

## 💳 Integracja płatności (PayU BLIK) <a name="platnosci"></a>

Implementacja: `SkiRental.Logic/Services/PaymentService.cs`

### Interface `IPaymentProvider`

```csharp
public interface IPaymentProvider
{
    Task<PaymentResult> InitiateBlikPayment(
        decimal amount, string blikCode, string orderId, string description);

    Task<PaymentResult> PreauthorizePayment(
        decimal amount, string orderId, string description);

    Task<PaymentResult> CapturePreauthorization(
        string externalTransactionId, decimal amount);

    Task<PaymentResult> RefundPayment(
        string externalTransactionId, decimal? partialAmount = null);

    Task<PaymentResult> CheckPaymentStatus(string externalTransactionId);
}
```

### Przepływ płatności BLIK

```
Klient podaje kod BLIK (6 cyfr)
        │
        ▼
BookingService.CreateBookingAsync()
        │
        ▼
IPaymentProvider.InitiateBlikPayment()
        │
        ├── PayU REST API: POST /api/v2_1/orders
        │   payload: payMethod.type = "PBL", value = "BLIK"
        │            authorizationCode = blikCode
        │
        ▼
PayU weryfikuje kod z bankiem klienta
        │
        ├─ SUCCESS → Rental.PaymentStatus = Paid
        └─ FAILURE → BookingResult.Failure → UI wyświetla błąd
```

### Środowiska PayU

| Środowisko  | BaseUrl                          |
|-------------|----------------------------------|
| Sandbox     | `https://secure.snd.payu.com`    |
| Produkcja   | `https://secure.payu.com`        |

---

## 📷 Panel Pracownika – QR/NFC <a name="qr-nfc"></a>

### Architektura skanera QR

```
MainPage.xaml (MAUI Native)
  └── ZXing CameraBarcodeReaderView
        │  OnBarcodesDetected()
        ▼
MainPage.xaml.cs
  └── InvokeBlazerQrCallback(qrCode)
        │  BlazorWebView.TryDispatchAsync()
        │  JS: skiRentalBridge.dispatchQrCode(qrCode)
        ▼
wwwroot/index.html
  └── skiRentalBridge.dispatchQrCode()
        │  dotNetRef.invokeMethodAsync("OnQrCodeDetected", qrCode)
        ▼
QrScannerPanel.razor [JSInvokable]
  └── OnQrCodeDetected(string qrCode)
        └── BookingService.ProcessReturnByQrCodeAsync()
```

### Rejestracja komponentu Blazor jako odbiorcy QR

```csharp
// W komponencie QrScannerPanel.razor:
[JSInvokable("OnQrCodeDetected")]
public async Task OnQrCodeDetected(string qrCode)
{
    ManualQrCode = qrCode;
    await ProcessQrCodeAsync();
    StateHasChanged();
}

// Rejestracja przez JS Interop:
await JS.InvokeVoidAsync("skiRentalBridge.startQrScanner",
    DotNetObjectReference.Create(this));
```

### NFC – zapis ID wypożyczenia na opaskę

```csharp
// QrNfcBridgeService.cs
await _bridge.WriteRentalToNfcTagAsync(rental.QrCode);
// → opaska klienta zawiera token QR → przy zwrocie wystarczy przyłożyć
```

---

## 📊 Analityka – UtilizationRate <a name="analityka"></a>

Implementacja: `SkiRental.Logic/Services/AnalyticsService.cs`

### Wzór

```
                  Σ (czas wypożyczeń sprzętu) [h]
UtilizationRate = ────────────────────────────────────────
                  Całkowity dostępny czas sezonu [h]
```

### Przykład

```csharp
var report = await analyticsService.CalculateUtilizationRateAsync(
    equipmentId: 3,
    seasonStart: new DateTime(2024, 12, 1),
    seasonEnd:   new DateTime(2025, 3, 31)
);

// report.TotalRentedHours     = 1440 h  (60 dni × 24h)
// report.SeasonAvailableHours = 2880 h  (120 dni × 24h)
// report.UtilizationRate      = 0.50    = 50%
// report.UtilizationRatePercent = 50.0%
// report.Revenue              = 4320 PLN
```

### KPI w dashboardzie

| KPI                      | Źródło                         |
|--------------------------|---------------------------------|
| Średni UtilizationRate   | `Avg(UtilizationRate × 100)`   |
| Przychód sezonu          | `Sum(Rental.TotalAmount)`      |
| Sprzęt < 30% użycia      | Kandydaci do wycofania z floty |
| Sprzęt wymagający serwisu | `LastMaintenanceDate < 30 dni` |

---

## 🗄️ Konfiguracja bazy danych <a name="baza-danych"></a>

### Rozwiązywanie problemów z bazą danych

**Problem:** "Błąd bazy danych. Spróbuj ponownie." przy logowaniu

**Przyczyny i rozwiązania:**
1. **Brak pliku bazy danych** - Aplikacja kopiuje bazę danych z `SkiRental.Logic/SkiRental.db` do katalogu danych aplikacji
2. **Brak tabel w bazie danych** - Migracje EF Core są automatycznie aplikowane przy starcie
3. **Błędna ścieżka** - Używana jest ścieżka bezwzględna do pliku bazy danych

**Debugowanie:**
- Dodano logowanie szczegółowe błędów bazy danych w `AuthenticationService.cs`
- Dodano logowanie ścieżki bazy danych w `MauiProgram.cs`
- Aplikacja tworzy kopię lokalną bazy danych przy każdym uruchomieniu

**Kroki naprawy:**
1. Usuń lokalny plik bazy danych: `rm SkiRental.db`
2. Odbuduj i uruchom aplikację: `dotnet build && dotnet run`
3. Sprawdź logi w konsoli aplikacji

### SQLite (mobilny / offline)

```csharp
// MauiProgram.cs – domyślna konfiguracja
builder.Services.AddDbContext<SkiRentalDbContext>(opts =>
    opts.UseSqlite($"Data Source={dbPath}"));
```

### SQL Server (back-office)

```csharp
// Dla aplikacji web/back-office – podmień w DI:
builder.Services.AddDbContext<SkiRentalDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("SkiRental")));
```

```json
// appsettings.json (back-office)
{
  "ConnectionStrings": {
    "SkiRental": "Server=.;Database=SkiRental;Trusted_Connection=True;"
  }
}
```

### PostgreSQL (opcjonalnie)

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

```csharp
opts.UseNpgsql(connectionString);
```

---

## 🧪 Testy jednostkowe <a name="testy"></a>

Implementacja: `SkiRental.Tests/EquipmentMathServiceTests.cs`

### Uruchomienie

```bash
cd SkiRental.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Pokrycie testami

| Test                                      | Weryfikuje                            |
|-------------------------------------------|---------------------------------------|
| `CalculateSkiLength_Classic_NormalBmi`    | Offset 20–30 cm wg poziomu            |
| `CalculateSkiLength_Skate_NormalBmi`      | Offset 10–15 cm wg poziomu            |
| `CalculateSkiLength_Overweight`           | Korekta BMI +5 cm                     |
| `CalculateSkiLength_Underweight`          | Korekta BMI −5 cm                     |
| `CalculatePoleLength_Classic`             | wzrost × 0.85 ± 2.5 cm               |
| `CalculatePoleLength_Skate`               | wzrost × 0.90 ± 2.5 cm               |
| `CalculateStiffness_BySkillLevel`         | Flex Index wg tabeli                  |
| `CalculateStiffness_AlwaysWithinBounds`   | Flex zawsze 30–90                     |
| `RecommendAsync_ReturnsExplanation`       | AlgorithmExplanation nie jest pusty   |
| `RecommendAsync_WithOverweight_BmiFlag`   | BmiCorrectionApplied = true           |

---

## 🗺️ Roadmap <a name="roadmap"></a>

### v1.1 – Sezon 2025/26
- [ ] Integracja z Google Maps (nawigacja do stacji narciarskiej)
- [ ] Push Notifications (przypomnienie o terminie zwrotu)
- [ ] Offline sync (SQLite → SQL Server po odzyskaniu łączności)
- [ ] Eksport raportów analitycznych do Excel / PDF

### v1.2
- [ ] Multi-tenant (obsługa wielu wypożyczalni w jednym systemie)
- [ ] Rezerwacja online z wyprzedzeniem (calendar view)
- [ ] Integracja z czytnikiem prawa jazdy/dowodu osobistego (weryfikacja tożsamości)
- [ ] AI chat assistant (GPT / Claude) – rekomendacje tras narciarskich

### v2.0
- [ ] Aplikacja watchOS / Wear OS dla pracownika (szybkie skanowanie QR)
- [ ] Blockchain – tokenizacja kaucji za sprzęt (smart contracts)

---

## 📄 Licencja

MIT © 2025 SkiRental Team

---

## 🤝 Kontakt

| Rola              | Kontakt                    |
|-------------------|----------------------------|
| Lead Architect    | architect@skirent.pl       |
| Backend Dev       | backend@skirent.pl         |
| Mobile Dev        | mobile@skirent.pl          |
