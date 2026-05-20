# SkiRental — Solution Documentation

This folder contains detailed documentation for every project in the **SkiRental** solution (`.NET 9` cross-country ski rental system).

## Solution overview

| Project | Type | Role |
|---------|------|------|
| [SkiRental.Domain](SkiRental.Domain.md) | Class library | Entities, enums, recommendation DTOs |
| [SkiRental.Logic](SkiRental.Logic.md) | Class library | EF Core, repositories, business services |
| [SkiRental.UI.Shared](SkiRental.UI.Shared.md) | Razor Class Library | Reusable Blazor components |
| [SkiRental.App](SkiRental.App.md) | MAUI Hybrid (Blazor) | Main client app (customers, workers, owners) |
| [SkiRental.Api](SkiRental.Api.md) | ASP.NET Core Web API | PayU webhook + health check |
| [SkiRental.Tests](SkiRental.Tests.md) | xUnit test project | Unit tests for Logic layer |

## Dependency graph

```
SkiRental.Domain
       ↑
SkiRental.Logic ──────┬──────────────┐
       ↑              │              │
SkiRental.UI.Shared   │         SkiRental.Api
       ↑              │
SkiRental.App         │
                      │
SkiRental.Tests ──────┘ (references Domain + Logic only)
```

## Cross-cutting documentation

| Document | Contents |
|----------|----------|
| [Architecture.md](Architecture.md) | End-to-end flows, roles, deployment |
| [Database-Schema.md](Database-Schema.md) | Tables, relationships, migrations |
| [Authentication-System.md](Authentication-System.md) | Login, JWT, route policies |
| [API-Documentation.md](API-Documentation.md) | Interfaces, services, Blazor components (not HTTP REST catalog) |

## Quick start

```bash
# From SkiRental/ directory
dotnet restore
dotnet build

# Run MAUI app (pick your target framework)
dotnet build SkiRental.App/SkiRental.App.csproj -f net9.0-windows10.0.19041.0

# Run API (PayU webhooks, migrations)
dotnet run --project SkiRental.Api

# Unit tests
dotnet test SkiRental.Tests
```

## Demo accounts (seeded in database)

| Username | Password | Role |
|----------|----------|------|
| `owner` | `owner123` | Owner |
| `worker` | `worker123` | Worker |
| `user` | `user123` | Customer |

## Technology stack

- **UI:** .NET MAUI + Blazor WebView, shared RCL
- **Data:** EF Core 9 — SQLite (mobile/local), SQL Server (optional)
- **Payments:** PayU REST (BLIK, pre-auth, capture, refund)
- **Device:** ZXing.Net.Maui (QR), Plugin.NFC
- **Weather:** OpenWeatherMap
- **Notifications:** MailKit (SMTP), Twilio (SMS), in-app inbox (DB)

## Document maintenance

When changing code, update the matching project doc and, if needed, `Architecture.md`, `Database-Schema.md`, or `Authentication-System.md`. Prefer documenting **behavior** and **contracts** over copying large code blocks.
