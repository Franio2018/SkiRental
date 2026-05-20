# SkiRental.Tests — Unit Test Project

## Overview

**SkiRental.Tests** validates critical business rules in **SkiRental.Logic** without loading the MAUI UI or ASP.NET hosts. It uses **xUnit**, **Moq**, and **FluentAssertions**.

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target | `net9.0` |
| `IsTestProject` | `true` |

---

## Project structure

```
SkiRental.Tests/
├── EquipmentMathServiceTests.cs   # Sizing algorithm (FIS-style)
├── RentalRulesServiceTests.cs     # Booking window + late fees
└── SkiRental.Tests.csproj
```

---

## Dependencies

| Reference / Package | Purpose |
|---------------------|---------|
| `SkiRental.Domain` | Enums, `EquipmentSelectionParameters` |
| `SkiRental.Logic` | Services under test |
| `xunit`, `xunit.runner.visualstudio` | Test framework |
| `Microsoft.NET.Test.Sdk` | Runner |
| `Moq` | Mock `IEquipmentRepository` |
| `FluentAssertions` | Readable assertions |

**Does not reference:** App, Api, UI.Shared (keeps tests fast and headless).

---

## Running tests

```bash
cd SkiRental
dotnet test SkiRental.Tests

# With verbosity
dotnet test SkiRental.Tests --logger "console;verbosity=detailed"
```

CI: `dotnet test` on solution or test project only.

---

## EquipmentMathServiceTests

**System under test (SUT):** `EquipmentMathService`

**Setup:** Mocks `IEquipmentRepository.GetAvailableByStyleAsync` to return empty list (tests focus on pure math, not inventory).

### Coverage areas

| Area | Technique | Examples |
|------|-----------|----------|
| Classic ski length | `[Theory]` + `[InlineData]` | Height/skill → expected cm |
| Skate ski length | Theory | Shorter offset than classic |
| BMI correction | Under/over/normal weight | ±5 cm on ski length |
| Pole length | Classic vs skate multipliers | 0.85 vs 0.90 × height |
| Stiffness | Skill levels + BMI | Flex index bounds |
| Explanation text | Single tests | Non-empty human explanation |

Assertions use FluentAssertions (`result.Should().Be(expected)`).

### Example pattern

```csharp
[Theory]
[InlineData(175, SkillLevel.Intermediate, SkiStyle.Classic, 200)] // illustrative
public void CalculateSkiLength_Classic_Intermediate_ReturnsExpected(
    int height, SkillLevel skill, SkiStyle style, decimal expected)
{
    var parameters = new EquipmentSelectionParameters { ... };
    var length = _sut.CalculateSkiLength(parameters);
    length.Should().Be(expected);
}
```

(Exact expected values match implementation rounding to 2.5 cm — see source for current matrices.)

---

## RentalRulesServiceTests

**SUT:** `RentalRulesService`

**Setup:** `IOptions<SkiRentalOptions>` with configurable season dates, `MaxRentalDaysPerBooking`, `LateFeePercentPerHour`.

### Coverage areas

| Test | Validates |
|------|-----------|
| Booking inside season | `ValidateBookingWindow` success |
| Booking outside season | Failure message |
| Duration over max days | Rejection (e.g. 14 days) |
| Late fee calculation | Percent per hour after `EndTime` |

---

## What is not tested (gaps)

| Component | Suggested future tests |
|-----------|------------------------|
| `BookingService` | Reservation race, cancel, return |
| `RecommendationOrchestrator` | No-match paths |
| `PayUPaymentProvider` | HTTP mocked handler |
| `EfRepositories` | In-memory `AddSkiRentalServices(..., useInMemory: true)` |
| `UserService` | Lockout, BCrypt verify |
| Blazor / MAUI | UI test project separate |

### Adding integration-style tests

```csharp
var services = new ServiceCollection();
services.AddSkiRentalServices(useInMemory: true);
var provider = services.BuildServiceProvider();
await provider.ApplyMigrationsAsync();
// resolve BookingService and run scenario
```

---

## Conventions

- Test class name: `{ClassUnderTest}Tests`
- One SUT per test class
- Arrange / Act / Assert with clear comments (Polish summaries in existing file headers)
- No shared mutable state between tests
- Mock only boundaries (repositories), not internal private methods

---

## Relationship to documentation

Algorithm constants and formulas are specified in:

- [SkiRental.Logic.md](SkiRental.Logic.md) — `EquipmentMathService`
- [SkiRental.Domain.md](SkiRental.Domain.md) — enums and parameters

When changing formulas, **update tests first or together** to prevent regressions.

---

## See also

- [SkiRental.Logic.md](SkiRental.Logic.md)
- Root [README.md](README.md) — `dotnet test` quick start
