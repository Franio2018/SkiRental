# SkiRental.Logic — Business Logic & Data Access

## Overview

**SkiRental.Logic** implements application services, EF Core persistence, external integrations (PayU, email, SMS), and dependency injection shared by **SkiRental.App** and **SkiRental.Api**. It references only **SkiRental.Domain**.

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target | `net9.0` |

---

## Project structure

```
SkiRental.Logic/
├── Interfaces/
│   └── IInterfaces.cs              # Repositories, IPaymentProvider, orchestrators
├── Options/
│   └── SkiRentalOptions.cs         # Section "SkiRental"
├── Services/
│   ├── EfRepositories.cs           # IUser, IEquipment, IRental
│   ├── EquipmentMathService.cs     # Sizing formulas ("Inteligentny Asystent")
│   ├── RecommendationOrchestrator.cs
│   ├── GearSwapService.cs
│   ├── BookingService.cs           # Rental lifecycle
│   ├── RentalRulesService.cs       # Season window, fees
│   ├── PaymentService.cs           # PayUOptions + PayUPaymentProvider
│   ├── UserService.cs              # BCrypt, lockout, registration
│   ├── AnalyticsService.cs
│   ├── AnalyticsExportService.cs
│   ├── NotificationService.cs      # SMTP + Twilio
│   └── NotificationInboxService.cs
├── SkiRentalDbContext.cs
├── SkiRentalDbContextFactory.cs    # Design-time EF
├── ServiceCollectionExtensions.cs  # AddSkiRentalServices
└── Migrations/
    ├── 20260510162110_InitialCreate.cs
    └── 20260520225803_AddNotificationsAndInventory.cs
```

`PayUPaymentProvider` and `PayUOptions` live in `PaymentService.cs` (same assembly).

---

## Composition: AddSkiRentalServices

```csharp
public static IServiceCollection AddSkiRentalServices(
    this IServiceCollection services,
    string? connectionString = null,
    bool useInMemory = false,
    IConfiguration? configuration = null)
```

| Registration | Lifetime | Implementation |
|--------------|----------|----------------|
| `SkiRentalDbContext` | Scoped | SQLite / SQL Server / InMemory |
| `IUserRepository` | Scoped | `EfUserRepository` |
| `IEquipmentRepository` | Scoped | `EfEquipmentRepository` |
| `IRentalRepository` | Scoped | `EfRentalRepository` |
| `EquipmentMathService` | Scoped | |
| `BookingService` | Scoped | |
| `AnalyticsService` | Scoped | |
| `UserService` | Scoped | |
| `IRecommendationOrchestrator` | Scoped | `RecommendationOrchestrator` |
| `IGearSwapService` | Scoped | `GearSwapService` |
| `INotificationService` | Scoped | `NotificationService` |
| `RentalRulesService` | Scoped | |
| `NotificationInboxService` | Scoped | |
| `AnalyticsExportService` | Scoped | |
| `SkiRentalOptions` | Options | From config section `SkiRental` |

**Not registered here:** `IPaymentProvider` — each host registers `PayUPaymentProvider` with `IHttpClientFactory`.

**Migrations helper:**

```csharp
await serviceProvider.ApplyMigrationsAsync();
```

---

## Database context

### SkiRentalDbContext

**DbSets:** `AppUsers`, `Equipment`, `UserProfiles`, `Rentals`, `RentalEquipments`, `Payments`, `AppNotifications`

**OnModelCreating highlights:**

- Unique index on `Equipment.SerialNumber`
- Unique index on `Rental.QrCode` (when present)
- Precision on `decimal` columns (rates, amounts, weight)
- Seed data: demo users (`owner`, `worker`, `user`) and sample equipment
- Relationships: rental → customer profile, rental-equipment M:N, payments

**Provider selection** (in `AddSkiRentalServices`):

- `useInMemory` → `UseInMemoryDatabase("SkiRentalTest")`
- Connection contains `Data Source=` or ends with `.db` → **SQLite**
- Otherwise → **SQL Server**

### Migrations

| Migration | Adds |
|-----------|------|
| `InitialCreate` | Core schema |
| `AddNotificationsAndInventory` | Notifications, inventory refinements |

Design-time factory: `SkiRentalDbContextFactory` — use startup project **App** or **Api**.

---

## Repository contracts

### IUserRepository

| Method | Description |
|--------|-------------|
| `GetByUsernameAsync` | Case-insensitive login lookup |
| `GetByIdAsync`, `GetAllAsync` | Admin / staff management |
| `CreateAsync`, `UpdateAsync`, `DeleteAsync` | Account CRUD |
| `UsernameExistsAsync` | Registration check |
| `GetProfileByUserIdAsync` | Linked `UserProfile` |
| `UpsertProfileAsync` | Create/update profile for user |

### IEquipmentRepository

| Method | Description |
|--------|-------------|
| `GetAvailableByStyleAsync` | Filter by `SkiStyle` + `EquipmentCategory` |
| `GetByIdAsync`, `GetAllAsync` | |
| `UpdateStatusAsync` | Status transitions |
| `GetByRentalIdAsync` | Equipment on a rental |
| `CreateAsync`, `UpdateAsync`, `ExistsAsync` | Fleet management |

### IRentalRepository

| Method | Description |
|--------|-------------|
| `GetByIdAsync`, `GetByQrCodeAsync` | Return flow |
| `GetActiveRentalsAsync`, `GetAllRentalsAsync` | Operations / analytics |
| `GetByCustomerIdAsync` | My rentals |
| `CreateAsync`, `UpdateAsync` | |
| `GetRentalsForUtilizationAsync` | Per-equipment season stats |
| `GetRentalSummariesByCustomerIdsAsync` | Batch summaries for management UI |

---

## Core services

### EquipmentMathService — intelligent assistant

Pure sizing math (FIS-inspired) before inventory matching.

**Ski length**

- Classic: `Height + interpolate(20..30 cm)` by skill
- Skate: `Height + interpolate(10..15 cm)` by skill
- BMI correction: ±5 cm if under/over normal BMI (18.5–24.9)
- Rounded to nearest 2.5 cm

**Pole length**

- Classic: `Height × 0.85`
- Skate: `Height × 0.90`

**Stiffness (flex index)**

| SkillLevel | Base flex |
|------------|-----------|
| Beginner | 42 |
| Intermediate | 52 |
| Advanced | 62 |
| Expert | 72 |

BMI adjustment ±5, clamped to sensible range.

**Public API:** `RecommendAsync`, `CalculateSkiLength`, `CalculatePoleLengthCm`, `CalculateStiffness`, explanation builders.

---

### RecommendationOrchestrator

Implements `IRecommendationOrchestrator`.

- Calls `EquipmentMathService` for targets
- For each category in `RentalEquipmentCategories.All`, queries available stock
- Applies tolerances (e.g. ±5 cm skis/poles)
- Sets `NoMatchReason` when inventory cannot satisfy
- Returns `RecommendationResult` for the page-based UI

---

### GearSwapService

Implements `IGearSwapService`.

- Returns `List<SwapOption>` for a category
- Excludes currently selected equipment id
- Scores alternatives within safety tolerance for customer-facing swap drawer

---

### BookingService — rental lifecycle

Central orchestrator; uses explicit transactions and `ExecuteUpdateAsync` for race-safe reservations.

#### Status flow

```
CreateBookingAsync     → Pending  + equipment Reserved
ActivateRentalAsync    → Active   + equipment Rented
ProcessReturnAsync     → Completed + equipment Available
CancelBookingAsync     → Cancelled + release equipment
```

#### CreateBookingAsync

1. `RentalRulesService.ValidateBookingWindow`
2. Serializable transaction
3. For each equipment id: atomic `Available` → `Reserved` (fails if race lost)
4. Calculate `TotalAmount` from daily rates × duration
5. Generate unique `QrCode`
6. Optional BLIK or pre-auth via `IPaymentProvider`
7. Persist rental + `RentalEquipment` rows
8. In-app notification via `NotificationInboxService`

#### ActivateRentalAsync

Worker confirms pickup: `Pending` → `Active`, equipment → `Rented`.

#### ProcessReturnAsync

- Resolve rental by QR
- Record `ConditionAtReturn`, late fees via rules
- Capture pre-auth or mark paid
- Equipment → `Available`, rental → `Completed`

#### DTOs (in `BookingService.cs`)

- `CreateBookingRequest`, `BookingResult`
- `ReturnResult`, `ActivationResult`, `CancelResult`

---

### RentalRulesService

Bound to `SkiRentalOptions`:

| Option | Typical use |
|--------|-------------|
| `SeasonStart`, `SeasonEnd` | Reject bookings outside season |
| `MaxRentalDaysPerBooking` | Default 14 |
| `LateFeePercentPerHour` | Overdue charges |
| `DamageSurchargeThresholdCondition` | Condition below threshold triggers surcharge |
| `DamageSurchargePercent` | % of rental or fixed logic |
| `SupportEmail`, `SupportPhone` | UI / notifications |

Methods: `ValidateBookingWindow`, late fee and damage surcharge calculators.

---

### PayUPaymentProvider (IPaymentProvider)

| Method | PayU operation |
|--------|----------------|
| `InitiateBlikPayment` | BLIK code charge |
| `PreauthorizePayment` | Hold funds |
| `CapturePreauthorization` | Capture on return |
| `RefundPayment` | Full/partial refund |
| `CheckPaymentStatus` | Poll order status |

Uses OAuth2 client credentials, REST JSON, `PayUOptions` (`PosId`, `SignatureKey`, `ClientId`, `ClientSecret`, `BaseUrl`, `NotifyUrl`).

Returns `PaymentResult` record (success/failure, external id, raw response).

---

### UserService

- Password hashing: **BCrypt** work factor 12
- Registration, password change
- Lockout after failed attempts (coordinates with `AppUser.FailedLoginAttempts`, `LockoutEnd`)
- Used by `AuthenticationService` in App via `IServiceScopeFactory` (singleton auth accessing scoped DbContext)

---

### AnalyticsService & AnalyticsExportService

- **Utilization:** rented hours / season hours per equipment → `UtilizationReport`
- Fleet KPIs, revenue aggregates
- Export helpers for owner dashboard (CSV-oriented helpers in export service)

---

### NotificationService & NotificationInboxService

| Service | Channel | Behavior |
|---------|---------|----------|
| `NotificationService` | SMTP (MailKit), Twilio SMS | Fail-soft; logs errors |
| `NotificationInboxService` | Database `AppNotifications` | Persist, mark read, list for user |

Triggered on booking events, worker overrides, etc.

---

## Configuration

### SkiRentalOptions (`Options/SkiRentalOptions.cs`)

Section name: `"SkiRental"` — see [SkiRental.App.md](SkiRental.App.md) for full `appsettings.json` example.

### PayUOptions

Section: `"PayU"` — registered in App/Api hosts, not inside `AddSkiRentalServices`.

---

## NuGet dependencies

| Package | Role |
|---------|------|
| `Microsoft.EntityFrameworkCore.*` | Sqlite, SqlServer, InMemory, Design |
| `Microsoft.Extensions.Configuration` | Options binding |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI |
| `Microsoft.Extensions.Logging.Abstractions` | Logging |
| `BCrypt.Net-Next` | Password hashing |
| `MailKit`, `MimeKit` | Email |
| `Twilio` | SMS |

---

## Error handling philosophy

- Services return **result records** (`BookingResult.Failure(message)`) for expected business failures
- Unexpected exceptions logged; transaction rollback on booking conflicts
- Payment provider returns `PaymentResult.Failure` with error codes/messages

---

## Testing

See [SkiRental.Tests.md](SkiRental.Tests.md). Logic is tested without MAUI:

- `EquipmentMathServiceTests` — formulas
- `RentalRulesServiceTests` — window and fees

Gaps: `BookingService`, PayU integration, repository integration tests.

---

## Extension points

| Abstraction | Swap implementation |
|-------------|---------------------|
| `IPaymentProvider` | Stripe, mock for tests |
| `INotificationService` | Push notifications |
| `IRecommendationOrchestrator` | ML-based recommender |

Keep domain rules in services; do not leak EF types into Domain.

---

## See also

- [SkiRental.Domain.md](SkiRental.Domain.md)
- [Database-Schema.md](Database-Schema.md)
- [API-Documentation.md](API-Documentation.md) — full interface listing
- [Architecture.md](Architecture.md)
