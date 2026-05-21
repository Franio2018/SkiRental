# SkiRental.Tests — Unit Test Project

## Overview

**SkiRental.Tests** validates critical business rules in **SkiRental.Logic** without loading the MAUI UI or ASP.NET hosts. It uses **xUnit**, **Moq**, **FluentAssertions**, and **SQLite in-memory** for integration tests.

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target | `net9.0` |
| `IsTestProject` | `true` |

---

## Project structure

```
SkiRental.Tests/
├── Infrastructure/
│   ├── SqliteTestDatabase.cs      # Shared SQLite in-memory + DI
│   ├── MockPaymentProvider.cs     # Configurable IPaymentProvider
│   └── FakeHttpMessageHandler.cs  # PayU HTTP mocking
├── EquipmentMathServiceTests.cs
├── RentalRulesServiceTests.cs
├── BookingServiceTests.cs           # Integration (SQLite)
├── RecommendationOrchestratorTests.cs
├── PayUPaymentProviderTests.cs
├── UserServiceTests.cs
├── EfRepositoryIntegrationTests.cs
└── UiTestsPlaceholder.cs            # Skipped — UI = separate project
```

---

## Coverage matrix

| Component | Status | Test file |
|-----------|--------|-----------|
| `EquipmentMathService` | Covered | `EquipmentMathServiceTests` — Theory + formulas |
| `RentalRulesService` | Covered | `RentalRulesServiceTests` — season, max days, fees |
| `BookingService` | Covered | `BookingServiceTests` — reserve, race, activate, return, cancel, BLIK rollback |
| `RecommendationOrchestrator` | Covered | `RecommendationOrchestratorTests` — Full, Partial, NoMatch |
| `PayUPaymentProvider` | Covered | `PayUPaymentProviderTests` — OAuth + BLIK/preauth/capture/refund via fake HTTP |
| `EfRepositories` | Covered | `EfRepositoryIntegrationTests` — user, equipment status, rental QR |
| `UserService` | Covered | `UserServiceTests` — BCrypt, lockout, validation |
| Blazor / MAUI UI | Not in this project | `UiTestsPlaceholder` (skipped) |

---

## Running tests

```bash
cd SkiRental
dotnet test SkiRental.Tests

dotnet test --filter "FullyQualifiedName~BookingService"
```

**Last run:** 47 passed, 1 skipped (UI placeholder).

---

## Infrastructure

### SqliteTestDatabase

- Connection: `Data Source={guid};Mode=Memory;Cache=Shared`
- Registers `SkiRentalDbContext`, `AddSkiRentalServices`, `MockPaymentProvider`
- `SeedRentalScenarioAsync()` — customer profile + skis/poles/boots

### MockPaymentProvider

Flags: `BlikSucceeds`, `PreauthSucceeds`, `CaptureSucceeds`, `RefundSucceeds`.

### FakeHttpMessageHandler

Routes PayU OAuth and order API calls to canned JSON responses.

---

## BookingService tests (highlights)

| Test | Verifies |
|------|----------|
| `CreateBookingAsync_ReservesEquipment` | `Pending` + `Reserved` equipment |
| `CreateBookingAsync_WhenEquipmentAlreadyReserved_Fails` | Second booking on same skis fails |
| `CreateBookingAsync_OutsideSeason_Fails` | `RentalRulesService` rejection |
| `ActivateRentalAsync_MovesEquipmentToRented` | `Active` + `Rented` |
| `ProcessReturnByQrCodeAsync_CompletesRental` | `Completed` + `Available` |
| `CancelBookingAsync_ReleasesReservedEquipment` | `Cancelled` + inventory freed |
| `CancelBookingAsync_ActiveRental_IsRejected` | Cannot cancel active rental |
| `CreateBookingAsync_BlikFailure_RollsBackReservation` | Payment fail → equipment `Available` |

---

## UI tests (future)

Use a separate project, for example:

- **bUnit** — isolated Blazor components from `SkiRental.UI.Shared`
- **Appium / MAUI UITest** — end-to-end on device/emulator

See skipped test `UiTests_LiveInSeparateProject` in `UiTestsPlaceholder.cs`.

---

## See also

- [SkiRental.Logic.md](SkiRental.Logic.md)
- [README.md](README.md)
