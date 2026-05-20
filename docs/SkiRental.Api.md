# SkiRental.Api — Web API Host

## Overview

**SkiRental.Api** is a minimal **ASP.NET Core 9** web application. It is **not** the backend for the MAUI client’s day-to-day CRUD operations. Its roles are:

1. Expose a **PayU IPN/webhook** endpoint so online payments can be confirmed server-side.
2. Provide a **health check** for monitoring.
3. Run **EF Core migrations** against a shared database when deploying a central SQLite/SQL Server instance.

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target | `net9.0` |
| Style | Minimal APIs (no controllers) |

---

## Project structure

```
SkiRental.Api/
├── Program.cs                 # Host setup, endpoints
├── appsettings.json           # Connection string, PayU, SkiRental options
├── Properties/
│   └── launchSettings.json    # Dev URLs (e.g. https://localhost:7xxx)
└── SkiRental.Api.csproj
```

**No** `Controllers/`, `Pages/`, or Swagger in the current codebase.

---

## Dependencies

| Reference | Purpose |
|-----------|---------|
| `SkiRental.Logic` | `AddSkiRentalServices`, `SkiRentalDbContext`, repositories, PayU |
| `Microsoft.EntityFrameworkCore.Design` | Migrations tooling at design time |

Transitive: `SkiRental.Domain`.

---

## Program.cs — startup sequence

```csharp
// 1. Register Logic layer + DB
builder.Services.AddSkiRentalServices(
    connectionString,
    configuration: builder.Configuration);

// 2. PayU HttpClient + scoped IPaymentProvider
builder.Services.AddHttpClient(nameof(PayUPaymentProvider));
builder.Services.AddScoped<IPaymentProvider>(...);
builder.Services.Configure<PayUOptions>(...);

// 3. Build app, migrate database
await db.Database.MigrateAsync();

// 4. Map endpoints
app.MapPost("/api/payu/notify", ...);
app.MapGet("/health", ...);

app.Run();
```

### Database connection

Uses `ConnectionStrings:SkiRentalSqlite` from `appsettings.json`, fallback:

`Data Source=SkiRental.db`

Same SQLite detection rules as Logic: paths with `Data Source=` or `.db` → SQLite; otherwise SQL Server.

---

## HTTP endpoints

### `POST /api/payu/notify`

**Purpose:** Receive PayU payment notifications (IPN).

**Handler behavior (current implementation):**

1. Read raw request body as string.
2. Log body at Information level.
3. If body contains `"COMPLETED"` (case-insensitive):
   - Load active rentals via `IRentalRepository.GetActiveRentalsAsync()`.
   - For each rental with `PaymentStatus == Preauthorized`, set `PaymentStatus = Paid` and `UpdateAsync`.
4. Return `200 OK` with empty body.

**Important limitations (document for operators):**

- No PayU signature verification in sample code — **must be added for production**.
- Does not parse JSON structurally; string contains check is fragile.
- Updates **all** matching active pre-authorized rentals, not a single `orderId` from the payload.

**Configuration:** Set `PayU:NotifyUrl` in App and Api config to the public URL of this endpoint, e.g.:

`https://your-server.example.com/api/payu/notify`

### `GET /health`

**Response:**

```json
{
  "status": "healthy",
  "service": "SkiRental.Api"
}
```

Use for load balancers, Kubernetes probes, or uptime checks.

---

## Configuration (`appsettings.json`)

| Section | Keys | Notes |
|---------|------|-------|
| `ConnectionStrings` | `SkiRentalSqlite`, `SkiRentalSqlServer` | Api uses Sqlite key in `Program.cs` |
| `PayU` | `PosId`, `SignatureKey`, `ClientId`, `ClientSecret`, `BaseUrl`, `NotifyUrl` | Sandbox default: `https://secure.snd.payu.com` |
| `SkiRental` | Season dates, fees, support contacts | Bound to `SkiRentalOptions` |
| `Logging` | Standard levels | |

Environment-specific overrides: `appsettings.Development.json` (if present), environment variables, user secrets.

---

## Relationship to SkiRental.App

| Concern | App | Api |
|---------|-----|-----|
| User interface | Yes | No |
| Booking / return | In-process Logic | No |
| SQLite file | Local app data directory | Server path or shared volume |
| PayU BLIK from UI | Yes (`IPaymentProvider`) | No |
| PayU webhook | Cannot receive on device | Yes |

Typical deployment: App uses sandbox PayU with NotifyUrl pointing to a deployed Api instance; both can share the same database file only if co-located (unusual for mobile) — production often uses SQL Server for Api and sync/async patterns, or webhook-only state updates.

---

## Running locally

```bash
cd SkiRental
dotnet run --project SkiRental.Api
```

Check `Properties/launchSettings.json` for `applicationUrl`. Expose via ngrok/cloudflare tunnel when testing PayU sandbox callbacks.

```bash
curl https://localhost:PORT/health
```

---

## EF Core migrations

Migrations are authored in **SkiRental.Logic**, not Api:

```bash
dotnet ef migrations add MigrationName \
  --project SkiRental.Logic \
  --startup-project SkiRental.Api

dotnet ef database update \
  --project SkiRental.Logic \
  --startup-project SkiRental.Api
```

Api startup also calls `MigrateAsync()` automatically.

---

## Security recommendations

| Topic | Recommendation |
|-------|----------------|
| PayU IPN | Verify `OpenPayu-Signature` header against second MD5 key |
| HTTPS | Required for NotifyUrl in production |
| Authentication | Add API key or mutual TLS if endpoint is public |
| Idempotency | Store processed notification ids to avoid double `Paid` |
| Scope updates | Map `order.extOrderId` / `PayUOrderId` to single rental |

---

## What is intentionally out of scope

- REST CRUD for equipment/rentals/users
- JWT bearer middleware for mobile clients
- Swagger / OpenAPI
- CORS for Blazor WASM (not used)

If a full remote API is added later, prefer a new `Controllers` folder or separate project; keep this host focused on webhooks or rename to `SkiRental.Webhooks`.

---

## See also

- [Architecture.md](Architecture.md) — payment flow diagram
- [SkiRental.Logic.md](SkiRental.Logic.md) — `PayUPaymentProvider`, `BookingService`
- [SkiRental.App.md](SkiRental.App.md) — client PayU configuration
