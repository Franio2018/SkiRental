# SkiRental.UI.Shared — Shared Blazor Components

## Overview

**SkiRental.UI.Shared** is a **Razor Class Library (RCL)** targeting `net9.0` only (no MAUI workload). It contains reusable Blazor components that depend on **SkiRental.Domain** and **SkiRental.Logic** for data and services, but **no MAUI or platform-specific code**.

The MAUI app loads this assembly via `Routes.razor` → `AdditionalAssemblies`.

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.Razor` |
| Target | `net9.0` |
| Root namespace | `SkiRental.UI.Shared` |

---

## Project structure

```
SkiRental.UI.Shared/
├── Components/
│   ├── Booking/
│   │   └── BookingWizard.razor          # Legacy 3-step wizard (single component)
│   ├── Review/
│   │   ├── GearItemCard.razor           # One line item in gear review
│   │   ├── SwapDrawer.razor             # Alternative equipment picker
│   │   └── AlgorithmExplanationBadge.razor
│   ├── Payment/
│   │   ├── PaymentMethodSelector.razor  # Spot vs online
│   │   └── OnlinePaymentPanel.razor     # BLIK entry
│   ├── Confirmation/
│   │   └── ReceiptSummary.razor
│   ├── Employee/
│   │   └── QrScannerPanel.razor         # Staff return UI + JS bridge
│   └── Analytics/
│       └── UtilizationDashboard.razor
└── SkiRental.UI.Shared.csproj
```

**Note:** Styles for booking/employee/analytics may live in **App** `wwwroot/css/` (`employee.css`, `analytics.css`, `new-rental-flow.css`) when not colocated in RCL `wwwroot`.

---

## Dependencies

| Project | Why |
|---------|-----|
| `SkiRental.Domain` | Models, enums, DTOs in parameters |
| `SkiRental.Logic` | Injected services (`BookingService`, `AnalyticsService`, …) |

| NuGet | Why |
|-------|-----|
| `Microsoft.AspNetCore.Components.Web` | Razor components |

**Does not reference** `SkiRental.App` — avoids circular dependency.

---

## Registration in the host

```razor
@* SkiRental.App/Routes.razor *@
<Router AppAssembly="@typeof(Routes).Assembly"
        AdditionalAssemblies="@ExtraAssemblies">

@code {
    private static readonly Assembly[] ExtraAssemblies =
    [
        typeof(BookingWizard).Assembly  // SkiRental.UI.Shared
    ];
}
```

Components are used by fully qualified name or `@using SkiRental.UI.Shared.Components.*`.

---

## Component catalog

### Booking — `BookingWizard.razor`

**Route usage:** `/booking` page can embed this wizard.

**Purpose:** All-in-one flow: profile data → recommendation → payment.

**Typical injections:**

- `EquipmentMathService` or `IRecommendationOrchestrator`
- `BookingService`
- `UserService`
- `IAuthenticationService`

**Steps (conceptual):**

1. Collect / confirm physical parameters
2. Show recommended gear and totals
3. Payment method + confirm booking

**Relation to App pages:** New flow splits steps across `CalendarBookingPage`, `GearReviewPage`, `PaymentPage`, etc., reusing **Review** and **Payment** components from this library.

---

### Review — gear selection UI

#### `GearItemCard.razor`

Displays one selected item: category icon, brand/model, size, daily rate, condition badge, “swap” action.

**Parameters (typical):** `GearSelection`, `OnSwapRequested` callback.

#### `SwapDrawer.razor`

Slide-over panel listing `SwapOption` from `IGearSwapService`.

- Loads alternatives when opened
- Emits selected equipment id to parent page state (`BookingStateService` in App)

#### `AlgorithmExplanationBadge.razor`

Compact tooltip or expandable panel showing why a size was recommended (ski cm, pole cm, stiffness, BMI note).

---

### Payment

#### `PaymentMethodSelector.razor`

Radio or card UI for `PaymentMethod.Spot` vs `PaymentMethod.Online`.

#### `OnlinePaymentPanel.razor`

BLIK code input (6 digits), validation, calls into parent to invoke `BookingService` / PayU on submit.

**Parent responsibility:** Pass `TotalAmount`, handle `BookingResult`, navigate to confirmation.

---

### Confirmation — `ReceiptSummary.razor`

Post-booking summary: rental id, QR preview text, dates, line items, payment status, link to “my rentals”.

---

### Employee — `QrScannerPanel.razor`

Staff-facing return panel.

**Integrations:**

- Subscribes to QR events via JS `skiRentalBridge` (dispatched from MAUI `MainPage`)
- Calls `BookingService.ProcessReturnAsync`
- Condition rating UI (1–5) per equipment line
- Shows success/error toasts (parent may inject `ToastService` from App)

**Does not open camera itself** — native scanner lives in `MainPage`; this component reacts to detected codes.

---

### Analytics — `UtilizationDashboard.razor`

Owner/worker dashboard fragment.

**Injects:** `AnalyticsService` (and optionally export helper)

**Displays:**

- Fleet utilization %
- Top/bottom performers
- Revenue summaries
- Date range filters (parameters from hosting `AnalyticsPage`)

---

## Dependency injection rules

| Service | Available in RCL? |
|---------|-------------------|
| `BookingService`, `EquipmentMathService`, … | Yes — registered in `MauiProgram` / host |
| `AuthenticationService` (App concrete) | Use `IAuthenticationService` in components |
| `BookingStateService` | App-only scoped — pass state via **parameters** from pages, do not inject in RCL |
| `QrNfcBridgeService` | App singleton — use JS interop or events, not direct inject in RCL |

**Best practice:** RCL components accept `EventCallback` and DTO parameters; pages in App wire `BookingStateService`.

---

## Styling

- Scoped CSS: `Component.razor.css` if present
- Global classes: `booking-*`, `employee-*`, `analytics-*` defined in App `wwwroot/css/`
- Keep components theme-agnostic where possible (CSS variables from App `app.css`)

---

## Adding a new shared component

1. Create under `Components/{Feature}/MyComponent.razor`
2. Inject only Logic interfaces or services
3. Add `@using` in App `_Imports.razor` if needed
4. Document parameters in [API-Documentation.md](API-Documentation.md)
5. Do **not** reference `Microsoft.Maui.*`

---

## Future: Blazor WebAssembly or Server host

Because the RCL targets `net9.0` without MAUI:

- A future `SkiRental.Web` project could reference the same RCL + Logic
- Replace `QrScannerPanel` with browser-based QR library on web
- Reuse `UtilizationDashboard`, `PaymentMethodSelector`, etc.

---

## See also

- [SkiRental.App.md](SkiRental.App.md) — pages that host these components
- [API-Documentation.md](API-Documentation.md) — parameter reference
- [Architecture.md](Architecture.md) — booking flow
