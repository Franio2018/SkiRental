# SkiRental API Documentation

## Overview

This document provides comprehensive API documentation for all public interfaces in the SkiRental system. It includes domain models, service interfaces, repository contracts, and component APIs.

## Table of Contents

1. [Domain Models API](#domain-models-api)
2. [Service Interfaces API](#service-interfaces-api)
3. [Repository Interfaces API](#repository-interfaces-api)
4. [Blazor Components API](#blazor-components-api)
5. [Authentication API](#authentication-api)
6. [Error Handling](#error-handling)
7. [Data Transfer Objects](#data-transfer-objects)
8. [Examples and Usage](#examples-and-usage)

---

## Domain Models API

### Equipment

**Namespace**: `SkiRental.Domain.Models`

#### Properties

| Property | Type | Access | Description | Constraints |
|----------|------|--------|-------------|-------------|
| `Id` | `int` | Get | Primary key identifier | Database-generated |
| `SerialNumber` | `string` | Get/Set | Unique manufacturer serial number | Required, Max 100, Unique |
| `Brand` | `string` | Get/Set | Equipment manufacturer | Max 100 |
| `Model` | `string` | Get/Set | Equipment model name | Max 100 |
| `Category` | `EquipmentCategory` | Get/Set | Equipment type category | Enum |
| `Size` | `decimal` | Get/Set | Size (cm or EU) | Decimal(5,1) |
| `Stiffness` | `int?` | Get/Set | Flex index (1-120) | Optional |
| `Status` | `EquipmentStatus` | Get/Set | Current status | Enum |
| `CompatibleStyle` | `SkiStyle?` | Get/Set | Compatible skiing style | Optional |
| `YearOfProduction` | `int` | Get/Set | Manufacturing year | Required |
| `DailyRate` | `decimal` | Get/Set | Daily rental price (PLN) | Decimal(8,2) |
| `TotalRentalDays` | `int` | Get/Set | Total days rented | Default 0 |
| `ImagePath` | `string?` | Get/Set | Equipment image path | Optional, Max 500 |
| `LastMaintenanceDate` | `DateTime?` | Get/Set | Last service date | Optional |
| `MaintenanceNotes` | `string?` | Get/Set | Service notes | Optional, Max 1000 |

#### Navigation Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `RentalEquipments` | `ICollection<RentalEquipment>` | Get | Rental associations |

#### Example

```csharp
var equipment = new Equipment
{
    SerialNumber = "FIS-2024-001",
    Brand = "Fischer",
    Model = "Carbonlite",
    Category = EquipmentCategory.Skis,
    Size = 185m,
    Stiffness = 52,
    Status = EquipmentStatus.Available,
    CompatibleStyle = SkiStyle.Classic,
    YearOfProduction = 2024,
    DailyRate = 65m
};
```

---

### UserProfile

**Namespace**: `SkiRental.Domain.Models`

#### Properties

| Property | Type | Access | Description | Constraints |
|----------|------|--------|-------------|-------------|
| `Id` | `int` | Get | Primary key | Database-generated |
| `FirstName` | `string` | Get/Set | First name | Required, Max 100 |
| `LastName` | `string` | Get/Set | Last name | Required, Max 100 |
| `Email` | `string?` | Get/Set | Email address | Optional, Max 200 |
| `PhoneNumber` | `string?` | Get/Set | Phone number | Optional, Max 20 |
| `Height` | `int` | Get/Set | Height in centimeters | Range 100-250 |
| `Weight` | `decimal` | Get/Set | Weight in kilograms | Decimal(5,1), Range 30-300 |
| `ShoeSize` | `int` | Get/Set | EU shoe size | Range 30-55 |
| `SkillLevel` | `SkillLevel` | Get/Set | Skiing proficiency | Enum |
| `PreferredStyle` | `SkiStyle` | Get/Set | Preferred skiing style | Enum |
| `DateOfBirth` | `DateTime?` | Get/Set | Birth date | Optional |
| `Address` | `string?` | Get/Set | Full address | Optional, Max 500 |
| `City` | `string?` | Get/Set | City name | Optional, Max 100 |
| `ProfileImagePath` | `string?` | Get/Set | Profile photo path | Optional, Max 500 |
| `FavoriteRoutes` | `string?` | Get/Set | Preferred ski routes | Optional, Max 1000 |
| `PreferenceNotes` | `string?` | Get/Set | Customer notes | Optional, Max 2000 |
| `EmailNotificationsEnabled` | `bool` | Get/Set | Email notification preference | Default true |
| `SmsNotificationsEnabled` | `bool` | Get/Set | SMS notification preference | Default false |
| `CreatedAt` | `DateTime` | Get/Set | Account creation date | UTC |
| `UpdatedAt` | `DateTime?` | Get/Set | Last update date | Optional |

#### Computed Properties (NotMapped)

| Property | Type | Access | Description | Formula |
|----------|------|--------|-------------|---------|
| `Bmi` | `decimal` | Get | Body Mass Index | `Weight / (Height/100)²` |
| `IsBmiNormal` | `bool` | Get | BMI in normal range | `18.5 ≤ BMI ≤ 24.9` |
| `IsUnderweight` | `bool` | Get | BMI below normal | `BMI < 18.5` |
| `IsOverweight` | `bool` | Get | BMI above normal | `BMI > 24.9` |
| `Age` | `int?` | Get | Calculated age | Current year - birth year |
| `TotalRentalsCount` | `int` | Get | Number of rentals | Count of Rentals |
| `LastRentalDate` | `DateTime?` | Get | Most recent rental | Max of Rental.StartTimes |
| `TotalSpent` | `decimal` | Get | Total money spent | Sum of paid rentals |
| `FavoriteEquipmentCategory` | `EquipmentCategory?` | Get | Most rented category | Mode of rented categories |

#### Navigation Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Rentals` | `ICollection<Rental>` | Get | User's rental history |

#### Example

```csharp
var user = new UserProfile
{
    FirstName = "Jan",
    LastName = "Kowalski",
    Email = "jan.kowalski@example.com",
    Height = 175,
    Weight = 75m,
    ShoeSize = 43,
    SkillLevel = SkillLevel.Intermediate,
    PreferredStyle = SkiStyle.Classic
};

// Computed values
// user.Bmi = 24.5
// user.IsBmiNormal = true
// user.IsOverweight = false
```

---

### Rental

**Namespace**: `SkiRental.Domain.Models`

#### Properties

| Property | Type | Access | Description | Constraints |
|----------|------|--------|-------------|-------------|
| `Id` | `int` | Get | Primary key | Database-generated |
| `CustomerProfileId` | `int` | Get/Set | Foreign key to UserProfile | Required |
| `EmployeeId` | `int?` | Get/Set | Employee handling rental | Optional |
| `StartTime` | `DateTime` | Get/Set | Rental start time | Required |
| `EndTime` | `DateTime` | Get/Set | Scheduled end time | Required |
| `ActualReturnTime` | `DateTime?` | Get/Set | Actual return time | Optional |
| `QrCode` | `string?` | Get/Set | QR code for return | Optional, Max 500, Unique |
| `NfcTagData` | `string?` | Get/Set | NFC tag data (Base64) | Optional, Max 1000 |
| `IsPreauthorized` | `bool` | Get/Set | Payment pre-authorized | Default false |
| `PaymentStatus` | `PaymentStatus` | Get/Set | Payment state | Enum |
| `TotalAmount` | `decimal` | Get/Set | Total rental cost | Decimal(10,2) |
| `PreauthorizedAmount` | `decimal?` | Get/Set | Pre-authorized amount | Optional, Decimal(10,2) |
| `PayUOrderId` | `string?` | Get/Set | PayU transaction ID | Optional, Max 200 |
| `Status` | `RentalStatus` | Get/Set | Rental status | Enum |
| `Notes` | `string?` | Get/Set | Rental notes | Optional, Max 500 |
| `CreatedAt` | `DateTime` | Get/Set | Creation timestamp | UTC |

#### Computed Properties (NotMapped)

| Property | Type | Access | Description | Formula |
|----------|------|--------|-------------|---------|
| `PlannedDurationHours` | `double` | Get | Planned rental duration | `(EndTime - StartTime).TotalHours` |

#### Navigation Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Customer` | `UserProfile` | Get/Set | Related user profile |
| `RentalEquipments` | `ICollection<RentalEquipment>` | Get | Equipment items |
| `Payments` | `ICollection<Payment>` | Get | Payment transactions |

#### Example

```csharp
var rental = new Rental
{
    CustomerProfileId = 1,
    StartTime = DateTime.Now,
    EndTime = DateTime.Now.AddHours(4),
    TotalAmount = 120m,
    Status = RentalStatus.Pending,
    QrCode = "QR-" + Guid.NewGuid().ToString("N")[..8].ToUpper()
};
```

---

## Service Interfaces API

### IEquipmentRepository

**Namespace**: `SkiRental.Logic.Interfaces`

#### Methods

##### GetByIdAsync

```csharp
Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default)
```

**Purpose**: Retrieve equipment by unique identifier

**Parameters**:
- `id` (int): Equipment ID
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<Equipment?>` - Equipment entity or null if not found

**Example**:
```csharp
var equipment = await repository.GetByIdAsync(123);
if (equipment != null)
{
    Console.WriteLine($"Found: {equipment.Brand} {equipment.Model}");
}
```

---

##### GetAllAsync

```csharp
Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default)
```

**Purpose**: Retrieve all equipment records

**Parameters**:
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Equipment>>` - All equipment

**Example**:
```csharp
var allEquipment = await repository.GetAllAsync();
foreach (var equipment in allEquipment)
{
    Console.WriteLine($"{equipment.Brand} {equipment.Model} - {equipment.Status}");
}
```

---

##### GetAvailableAsync

```csharp
Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken ct = default)
```

**Purpose**: Get only equipment that is currently available for rental

**Parameters**:
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Equipment>>` - Available equipment

**Example**:
```csharp
var available = await repository.GetAvailableAsync();
Console.WriteLine($"Available equipment: {available.Count}");
```

---

##### GetAvailableByStyleAsync

```csharp
Task<IReadOnlyList<Equipment>> GetAvailableByStyleAsync(
    SkiStyle style, 
    EquipmentCategory category, 
    CancellationToken ct = default)
```

**Purpose**: Get available equipment filtered by skiing style and category

**Parameters**:
- `style` (SkiStyle): Skiing style (Classic, Skate, Combi)
- `category` (EquipmentCategory): Equipment category
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Equipment>>` - Filtered available equipment

**Example**:
```csharp
var classicSkis = await repository.GetAvailableByStyleAsync(
    SkiStyle.Classic, 
    EquipmentCategory.Skis);
```

---

##### CreateAsync

```csharp
Task<Equipment> CreateAsync(Equipment equipment, CancellationToken ct = default)
```

**Purpose**: Add new equipment to the system

**Parameters**:
- `equipment` (Equipment): Equipment entity to create
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<Equipment>` - Created equipment with assigned ID

**Example**:
```csharp
var newEquipment = new Equipment
{
    SerialNumber = "TEST-001",
    Brand = "TestBrand",
    Model = "TestModel",
    Category = EquipmentCategory.Skis,
    Size = 180m,
    DailyRate = 50m
};

var created = await repository.CreateAsync(newEquipment);
Console.WriteLine($"Created equipment with ID: {created.Id}");
```

---

##### UpdateAsync

```csharp
Task<Equipment> UpdateAsync(Equipment equipment, CancellationToken ct = default)
```

**Purpose**: Update existing equipment information

**Parameters**:
- `equipment` (Equipment): Equipment entity with updated values
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<Equipment>` - Updated equipment

**Example**:
```csharp
equipment.Status = EquipmentStatus.Maintenance;
equipment.LastMaintenanceDate = DateTime.Now;
var updated = await repository.UpdateAsync(equipment);
```

---

##### DeleteAsync

```csharp
Task DeleteAsync(int id, CancellationToken ct = default)
```

**Purpose**: Remove equipment from the system

**Parameters**:
- `id` (int): Equipment ID to delete
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task` - Completion task

**Example**:
```csharp
await repository.DeleteAsync(123);
```

---

### IRentalRepository

**Namespace**: `SkiRental.Logic.Interfaces`

#### Methods

##### GetByIdAsync

```csharp
Task<Rental?> GetByIdAsync(int id, CancellationToken ct = default)
```

**Purpose**: Retrieve rental by unique identifier

**Parameters**:
- `id` (int): Rental ID
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<Rental?>` - Rental entity or null

---

##### GetByCustomerIdAsync

```csharp
Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
```

**Purpose**: Get all rentals for a specific customer

**Parameters**:
- `customerId` (int): Customer profile ID
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Rental>>` - Customer's rental history

---

##### GetActiveRentalsAsync

```csharp
Task<IReadOnlyList<Rental>> GetActiveRentalsAsync(CancellationToken ct = default)
```

**Purpose**: Get all currently active rentals

**Parameters**:
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Rental>>` - Active rentals

---

##### GetOverdueRentalsAsync

```csharp
Task<IReadOnlyList<Rental>> GetOverdueRentalsAsync(CancellationToken ct = default)
```

**Purpose**: Get rentals that are past their scheduled return time

**Parameters**:
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Rental>>` - Overdue rentals

---

##### CreateAsync

```csharp
Task<Rental> CreateAsync(Rental rental, CancellationToken ct = default)
```

**Purpose**: Create new rental record

**Parameters**:
- `rental` (Rental): Rental entity to create
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<Rental>` - Created rental

---

##### UpdateAsync

```csharp
Task<Rental> UpdateAsync(Rental rental, CancellationToken ct = default)
```

**Purpose**: Update existing rental information

**Parameters**:
- `rental` (Rental): Rental entity with updated values
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<Rental>` - Updated rental

---

##### GetRentalsByDateRangeAsync

```csharp
Task<IReadOnlyList<Rental>> GetRentalsByDateRangeAsync(
    DateTime start, 
    DateTime end, 
    CancellationToken ct = default)
```

**Purpose**: Get rentals within a specific date range

**Parameters**:
- `start` (DateTime): Start date
- `end` (DateTime): End date
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<IReadOnlyList<Rental>>` - Rentals in date range

---

### IPaymentProvider

**Namespace**: `SkiRental.Logic.Interfaces`

#### Methods

##### InitiateBlikPayment

```csharp
Task<PaymentResult> InitiateBlikPayment(
    decimal amount, 
    string blikCode, 
    string orderId, 
    string description)
```

**Purpose**: Process immediate BLIK mobile payment

**Parameters**:
- `amount` (decimal): Payment amount
- `blikCode` (string): 6-digit BLIK code
- `orderId` (string): Order identifier
- `description` (string): Payment description

**Returns**: `Task<PaymentResult>` - Payment result

**Example**:
```csharp
var result = await paymentProvider.InitiateBlikPayment(
    amount: 120m,
    blikCode: "123456",
    orderId: "ORDER-001",
    description: "Wypożyczenie sprzętu narciarskiego");

if (result.Success)
{
    Console.WriteLine($"Payment successful: {result.TransactionId}");
}
```

---

##### PreauthorizePayment

```csharp
Task<PaymentResult> PreauthorizePayment(
    decimal amount, 
    string orderId, 
    string description)
```

**Purpose**: Hold funds for later capture (rental deposit)

**Parameters**:
- `amount` (decimal): Amount to hold
- `orderId` (string): Order identifier
- `description` (string): Payment description

**Returns**: `Task<PaymentResult>` - Pre-authorization result

---

##### CapturePreauthorization

```csharp
Task<PaymentResult> CapturePreauthorization(
    string externalTransactionId, 
    decimal amount)
```

**Purpose**: Finalize pre-authorized payment

**Parameters**:
- `externalTransactionId` (string): Transaction ID from pre-authorization
- `amount` (decimal): Final amount to capture

**Returns**: `Task<PaymentResult>` - Capture result

---

##### RefundPayment

```csharp
Task<PaymentResult> RefundPayment(
    string externalTransactionId, 
    decimal? partialAmount = null)
```

**Purpose**: Process refund (full or partial)

**Parameters**:
- `externalTransactionId` (string): Original transaction ID
- `partialAmount` (decimal?): Partial refund amount (null for full refund)

**Returns**: `Task<PaymentResult>` - Refund result

---

##### CheckPaymentStatus

```csharp
Task<PaymentResult> CheckPaymentStatus(string externalTransactionId)
```

**Purpose**: Verify current payment status

**Parameters**:
- `externalTransactionId` (string): Transaction ID to check

**Returns**: `Task<PaymentResult>` - Current payment status

---

## Repository Interfaces API

### EquipmentMathService

**Namespace**: `SkiRental.Logic.Services`

#### Methods

##### RecommendAsync

```csharp
Task<EquipmentRecommendation> RecommendAsync(
    EquipmentSelectionParameters parameters, 
    CancellationToken ct = default)
```

**Purpose**: Generate complete equipment recommendation with available inventory

**Parameters**:
- `parameters` (EquipmentSelectionParameters): User physical data and preferences
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<EquipmentRecommendation>` - Complete recommendation

**Example**:
```csharp
var parameters = new EquipmentSelectionParameters
{
    Height = 175,
    Weight = 75m,
    ShoeSize = 43,
    Style = SkiStyle.Classic,
    SkillLevel = SkillLevel.Intermediate
};

var recommendation = await mathService.RecommendAsync(parameters);

Console.WriteLine($"Ski Length: {recommendation.RecommendedSkiLength} cm");
Console.WriteLine($"Pole Length: {recommendation.RecommendedPoleLength} cm");
Console.WriteLine($"Stiffness: {recommendation.RecommendedStiffness}");
Console.WriteLine($"Matching Equipment: {recommendation.MatchingEquipment.Count}");
```

---

##### CalculateSkiLength

```csharp
decimal CalculateSkiLength(EquipmentSelectionParameters parameters)
```

**Purpose**: Calculate optimal ski length based on user data

**Parameters**:
- `parameters` (EquipmentSelectionParameters): User data

**Returns**: `decimal` - Recommended ski length in centimeters

**Formula**:
- Classic: `Height + (20-30cm)` based on skill level
- Skate: `Height + (10-15cm)` based on skill level
- BMI correction: `±5cm` for under/overweight users

---

##### CalculatePoleLengthCm

```csharp
decimal CalculatePoleLengthCm(EquipmentSelectionParameters parameters)
```

**Purpose**: Calculate optimal pole length

**Parameters**:
- `parameters` (EquipmentSelectionParameters): User data

**Returns**: `decimal` - Recommended pole length in centimeters

**Formula**:
- Classic: `Height × 0.85`
- Skate: `Height × 0.90`

---

##### CalculateStiffness

```csharp
int CalculateStiffness(EquipmentSelectionParameters parameters)
```

**Purpose**: Calculate optimal ski stiffness (Flex Index)

**Parameters**:
- `parameters` (EquipmentSelectionParameters): User data

**Returns**: `int` - Recommended stiffness (30-90 range)

**Formula**: Base stiffness by skill level + BMI adjustment

---

### BookingService

**Namespace**: `SkiRental.Logic.Services`

#### Methods

##### CreateBookingAsync

```csharp
Task<BookingResult> CreateBookingAsync(
    CreateBookingRequest request, 
    CancellationToken ct = default)
```

**Purpose**: Create new rental booking with equipment assignment and payment

**Parameters**:
- `request` (CreateBookingRequest): Booking details
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<BookingResult>` - Booking result with confirmation

---

##### ProcessReturnAsync

```csharp
Task<ReturnResult> ProcessReturnAsync(
    string qrCode, 
    ReturnConditionData conditionData,
    CancellationToken ct = default)
```

**Purpose**: Process equipment return with condition evaluation

**Parameters**:
- `qrCode` (string): QR code from rental
- `conditionData` (ReturnConditionData): Equipment condition ratings
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<ReturnResult>` - Return processing result

---

##### CancelBookingAsync

```csharp
Task<CancellationResult> CancelBookingAsync(
    int rentalId, 
    CancellationToken ct = default)
```

**Purpose**: Cancel active booking with refund processing

**Parameters**:
- `rentalId` (int): Rental ID to cancel
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<CancellationResult>` - Cancellation result

---

### AnalyticsService

**Namespace**: `SkiRental.Logic.Services`

#### Methods

##### CalculateUtilizationRateAsync

```csharp
Task<UtilizationReport> CalculateUtilizationRateAsync(
    int equipmentId, 
    DateTime seasonStart, 
    DateTime seasonEnd,
    CancellationToken ct = default)
```

**Purpose**: Calculate equipment utilization rate for a specific period

**Parameters**:
- `equipmentId` (int): Equipment ID
- `seasonStart` (DateTime): Season start date
- `seasonEnd` (DateTime): Season end date
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<UtilizationReport>` - Utilization metrics

**Formula**: `UtilizationRate = Σ(RentalHours) / TotalSeasonHours`

---

##### GetFleetAnalyticsAsync

```csharp
Task<FleetAnalyticsReport> GetFleetAnalyticsAsync(
    DateTime startDate, 
    DateTime endDate,
    CancellationToken ct = default)
```

**Purpose**: Get comprehensive fleet analytics

**Parameters**:
- `startDate` (DateTime): Analysis start date
- `endDate` (DateTime): Analysis end date
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<FleetAnalyticsReport>` - Fleet performance metrics

---

##### GetCustomerAnalyticsAsync

```csharp
Task<CustomerAnalyticsReport> GetCustomerAnalyticsAsync(
    DateTime startDate, 
    DateTime endDate,
    CancellationToken ct = default)
```

**Purpose**: Get customer behavior analytics

**Parameters**:
- `startDate` (DateTime): Analysis start date
- `endDate` (DateTime): Analysis end date
- `ct` (CancellationToken): Cancellation token

**Returns**: `Task<CustomerAnalyticsReport>` - Customer insights

---

## Blazor Components API

### BookingWizard

**Namespace**: `SkiRental.UI.Shared.Components.Booking`

#### Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `OnBookingCompleted` | `EventCallback<BookingResult>` | Fired on successful booking | Required |
| `OnBookingCancelled` | `EventCallback` | Fired when user cancels | Optional |
| `DefaultRentalDuration` | `TimeSpan` | Default rental period | 4 hours |
| `ShowAdvancedOptions` | `bool` | Display advanced options | false |
| `EnableRecommendations` | `bool` | Use AI recommendations | true |

#### Methods

##### ResetWizard

```csharp
public void ResetWizard()
```

**Purpose**: Reset wizard to initial state

**Example**:
```razor
<BookingWizard @ref="bookingWizard" />

<button @onclick="bookingWizard.ResetWizard">Reset</button>
```

---

##### ValidateCurrentStep

```csharp
public bool ValidateCurrentStep()
```

**Purpose**: Validate current wizard step

**Returns**: `bool` - True if valid

---

### QrScannerPanel

**Namespace**: `SkiRental.UI.Shared.Components.Employee`

#### Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `OnReturnProcessed` | `EventCallback<ReturnResult>` | Fired on successful return | Required |
| `OnErrorOccurred` | `EventCallback<string>` | Fired on errors | Optional |
| `EnableNfcScanning` | `bool` | Enable NFC tag reading | true |
| `AutoFocusScanner` | `bool` | Auto-focus camera | true |
| `ShowAdvancedOptions` | `bool` | Display advanced options | false |

#### Methods

##### StartScanning

```csharp
public async Task StartScanning()
```

**Purpose**: Start QR code scanning

---

##### StopScanning

```csharp
public async Task StopScanning()
```

**Purpose**: Stop QR code scanning

---

##### ProcessManualQrCode

```csharp
public async Task ProcessManualQrCode(string qrCode)
```

**Purpose**: Process manually entered QR code

**Parameters**:
- `qrCode` (string): QR code to process

---

### UtilizationDashboard

**Namespace**: `SkiRental.UI.Shared.Components.Analytics`

#### Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `DateRange` | `DateRange` | Analytics date range | Last 30 days |
| `OnDateRangeChanged` | `EventCallback<DateRange>` | Fired when date range changes | Optional |
| `RefreshInterval` | `int` | Auto-refresh interval (ms) | 30000 |
| `ShowDetailedMetrics` | `bool` | Show detailed breakdowns | true |
| `EnableExport` | `bool` | Enable data export | false |

#### Methods

##### RefreshData

```csharp
public async Task RefreshData()
```

**Purpose**: Manually refresh dashboard data

---

##### ExportToExcel

```csharp
public async Task ExportToExcel()
```

**Purpose**: Export analytics data to Excel format

---

##### ExportToPdf

```csharp
public async Task ExportToPdf()
```

**Purpose**: Export analytics data to PDF format

---

## Authentication API

### AuthenticationService

**Namespace**: `SkiRental.App.Services`

#### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `IsAuthenticated` | `bool` | Get | User authentication status |
| `CurrentUsername` | `string?` | Get | Current user's display name |
| `CurrentUserId` | `string?` | Get | Current user's ID |
| `CurrentRole` | `UserRole` | Get | Current user's role |

#### Events

```csharp
public event Action? AuthenticationChanged;
```

**Purpose**: Fired when authentication state changes

#### Methods

##### LoginAsync

```csharp
Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
```

**Purpose**: Authenticate user with credentials

**Parameters**:
- `username` (string): Username or email
- `password` (string): User password
- `rememberMe` (bool): Remember login preference

**Returns**: `Task<LoginResult>` - Login result

**Example**:
```csharp
var result = await authService.LoginAsync("admin", "password", true);

if (result.Success)
{
    Console.WriteLine($"Logged in as: {authService.CurrentUsername}");
    Console.WriteLine($"Role: {authService.CurrentRole}");
}
```

---

##### Logout

```csharp
void Logout()
```

**Purpose**: Logout current user and clear authentication state

**Example**:
```csharp
authService.Logout();
Console.WriteLine($"Is authenticated: {authService.IsAuthenticated}"); // false
```

---

##### RegisterAsync

```csharp
Task<RegisterResult> RegisterAsync(
    string username, 
    string email, 
    string password, 
    string confirmPassword)
```

**Purpose**: Register new user account

**Parameters**:
- `username` (string): Desired username
- `email` (string): Email address
- `password` (string): Password
- `confirmPassword` (string): Password confirmation

**Returns**: `Task<RegisterResult>` - Registration result

---

## Error Handling

### Common Result Types

#### LoginResult

```csharp
public record LoginResult(bool Success, string Message);
```

#### RegisterResult

```csharp
public record RegisterResult(bool Success, string Message);
```

#### PaymentResult

```csharp
public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}
```

#### BookingResult

```csharp
public class BookingResult
{
    public bool Success { get; set; }
    public int RentalId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
```

#### ReturnResult

```csharp
public class ReturnResult
{
    public bool Success { get; set; }
    public int RentalId { get; set; }
    public string Receipt { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

### Error Codes

| Code | Description | Severity |
|------|-------------|----------|
| `EQUIPMENT_NOT_FOUND` | Equipment doesn't exist | Error |
| `EQUIPMENT_NOT_AVAILABLE` | Equipment is currently rented | Error |
| `INVALID_USER_DATA` | User profile validation failed | Error |
| `PAYMENT_FAILED` | Payment processing failed | Error |
| `QR_CODE_INVALID` | QR code not found or expired | Warning |
| `RENTAL_OVERDUE` | Rental is overdue | Warning |
| `INSUFFICIENT_PERMISSIONS` | User lacks required permissions | Error |

---

## Data Transfer Objects

### EquipmentSelectionParameters

```csharp
public class EquipmentSelectionParameters
{
    public int Height { get; set; }
    public decimal Weight { get; set; }
    public int ShoeSize { get; set; }
    public SkillLevel SkillLevel { get; set; }
    public SkiStyle Style { get; set; }
    
    // Computed properties
    public decimal Bmi => Height > 0 
        ? Math.Round(Weight / ((decimal)(Height / 100.0) * (decimal)(Height / 100.0)), 1) 
        : 0;
    
    public bool IsOverweight => Bmi > 24.9m;
    public bool IsUnderweight => Bmi < 18.5m;
}
```

### EquipmentRecommendation

```csharp
public class EquipmentRecommendation
{
    public decimal RecommendedSkiLength { get; set; }
    public decimal RecommendedPoleLength { get; set; }
    public int RecommendedStiffness { get; set; }
    public int RecommendedBootSize { get; set; }
    public SkiStyle Style { get; set; }
    public SkillLevel SkillLevel { get; set; }
    public bool BmiCorrectionApplied { get; set; }
    public string? BmiCorrectionNote { get; set; }
    public string AlgorithmExplanation { get; set; } = string.Empty;
    public IReadOnlyList<Equipment> MatchingEquipment { get; set; } = new List<Equipment>().AsReadOnly();
}
```

### CreateBookingRequest

```csharp
public class CreateBookingRequest
{
    public int CustomerProfileId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<int> EquipmentIds { get; set; } = new();
    public PaymentMethod PaymentMethod { get; set; }
    public string? BlikCode { get; set; }
    public string Notes { get; set; } = string.Empty;
}
```

### UtilizationReport

```csharp
public class UtilizationReport
{
    public int EquipmentId { get; set; }
    public decimal UtilizationRate { get; set; }
    public decimal UtilizationRatePercent => UtilizationRate * 100;
    public double TotalRentedHours { get; set; }
    public double SeasonAvailableHours { get; set; }
    public decimal Revenue { get; set; }
    public DateTime SeasonStart { get; set; }
    public DateTime SeasonEnd { get; set; }
}
```

---

## Examples and Usage

### Complete Booking Workflow

```csharp
// 1. Get user equipment recommendations
var parameters = new EquipmentSelectionParameters
{
    Height = 175,
    Weight = 75m,
    ShoeSize = 43,
    Style = SkiStyle.Classic,
    SkillLevel = SkillLevel.Intermediate
};

var recommendation = await equipmentMathService.RecommendAsync(parameters);

// 2. Create booking request
var bookingRequest = new CreateBookingRequest
{
    CustomerProfileId = userId,
    StartTime = DateTime.Now,
    EndTime = DateTime.Now.AddHours(4),
    EquipmentIds = recommendation.MatchingEquipment.Take(3).Select(e => e.Id).ToList(),
    PaymentMethod = PaymentMethod.Blik,
    BlikCode = "123456"
};

// 3. Process booking
var bookingResult = await bookingService.CreateBookingAsync(bookingRequest);

if (bookingResult.Success)
{
    Console.WriteLine($"Booking created! QR Code: {bookingResult.QrCode}");
    Console.WriteLine($"Total amount: {bookingResult.TotalAmount:C}");
}
```

### Equipment Return Processing

```csharp
// 1. Scan QR code (simulated)
string qrCode = "QR-ABC123";

// 2. Get condition data
var conditionData = new ReturnConditionData
{
    EquipmentConditions = new Dictionary<int, int>
    {
        { 1, 5 }, // Equipment ID 1: Excellent condition
        { 2, 4 }, // Equipment ID 2: Good condition
        { 3, 5 }  // Equipment ID 3: Excellent condition
    }
};

// 3. Process return
var returnResult = await bookingService.ProcessReturnAsync(qrCode, conditionData);

if (returnResult.Success)
{
    Console.WriteLine($"Return processed! Final amount: {returnResult.FinalAmount:C}");
    Console.WriteLine($"Receipt: {returnResult.Receipt}");
}
```

### Analytics Reporting

```csharp
// 1. Calculate equipment utilization
var utilizationReport = await analyticsService.CalculateUtilizationRateAsync(
    equipmentId: 1,
    seasonStart: new DateTime(2024, 12, 1),
    seasonEnd: new DateTime(2025, 3, 31)
);

Console.WriteLine($"Utilization Rate: {utilizationReport.UtilizationRatePercent:P1}");
Console.WriteLine($"Revenue: {utilizationReport.Revenue:C}");

// 2. Get fleet analytics
var fleetReport = await analyticsService.GetFleetAnalyticsAsync(
    startDate: DateTime.Today.AddDays(-30),
    endDate: DateTime.Today
);

Console.WriteLine($"Overall Utilization: {fleetReport.OverallUtilizationRate:P1}");
Console.WriteLine($"Total Revenue: {fleetReport.TotalRevenue:C}");
Console.WriteLine($"Equipment in Maintenance: {fleetReport.EquipmentInMaintenance}");
```

### Authentication Flow

```csharp
// 1. Login user
var loginResult = await authService.LoginAsync("admin", "password");

if (loginResult.Success)
{
    Console.WriteLine($"Authenticated: {authService.IsAuthenticated}");
    Console.WriteLine($"User: {authService.CurrentUsername}");
    Console.WriteLine($"Role: {authService.CurrentRole}");
    
    // 2. Check permissions
    if (authService.CurrentRole == UserRole.Worker)
    {
        // Allow access to worker features
        Console.WriteLine("Access granted to worker features");
    }
    
    // 3. Logout when done
    authService.Logout();
    Console.WriteLine($"Logged out: {!authService.IsAuthenticated}");
}
```

---

## API Versioning

### Current Version: 1.0

### Version History

| Version | Changes | Breaking Changes |
|---------|---------|------------------|
| 1.0 | Initial API release | - |
| 1.1 | Added analytics endpoints | None |
| 1.2 | Enhanced payment options | None |
| 1.3 | Added NFC support | None |

### Future Versions

#### Planned Additions (v2.0)

- Advanced filtering options
- Bulk operations
- Real-time notifications
- Enhanced analytics metrics

#### Breaking Changes Consideration

- Enum value changes
- Property type modifications
- Method signature updates

---

## Rate Limiting and Throttling

### Current Limits

| Endpoint | Requests/Minute | Burst Limit |
|----------|-----------------|-------------|
| Authentication | 60 | 10 |
| Equipment Search | 120 | 20 |
| Booking Creation | 30 | 5 |
| Analytics Queries | 60 | 10 |

### Rate Limit Headers

```http
X-RateLimit-Limit: 120
X-RateLimit-Remaining: 119
X-RateLimit-Reset: 1640995200
```

---

## Security Considerations

### Authentication

- All API endpoints require authentication except login/register
- JWT tokens used for session management
- Token expiration: 24 hours

### Authorization

- Role-based access control (RBAC)
- Worker role required for analytics and scanner features
- Customer role limited to booking and profile management

### Data Protection

- All sensitive data encrypted in transit
- PII masked in logs
- GDPR compliance for customer data

---

## Testing the API

### Unit Tests

```csharp
[Fact]
public async Task EquipmentMathService_CalculateSkiLength_ReturnsCorrectValue()
{
    // Arrange
    var service = new EquipmentMathService(mockRepo.Object, mockLogger.Object);
    var parameters = new EquipmentSelectionParameters
    {
        Height = 175,
        Weight = 75m,
        Style = SkiStyle.Classic,
        SkillLevel = SkillLevel.Intermediate,
        ShoeSize = 43
    };

    // Act
    var result = service.CalculateSkiLength(parameters);

    // Assert
    result.Should().BeApproximately(200m, 0.1m); // 175 + 25 (intermediate)
}
```

### Integration Tests

```csharp
[Fact]
public async Task BookingService_CreateBooking_WithValidData_ReturnsSuccess()
{
    // Arrange
    var service = new BookingService(mockRepo.Object, mockPayment.Object, mockLogger.Object);
    var request = CreateValidBookingRequest();

    // Act
    var result = await service.CreateBookingAsync(request);

    // Assert
    result.Success.Should().BeTrue();
    result.RentalId.Should().BeGreaterThan(0);
}
```

---

## SDK and Client Libraries

### .NET Client Library

```csharp
// NuGet: SkiRental.Client
var client = new SkiRentalClient("https://api.skirent.pl", "your-api-key");

// Authentication
await client.AuthenticateAsync("username", "password");

// Equipment search
var equipment = await client.Equipment.GetAvailableAsync(SkiStyle.Classic, EquipmentCategory.Skis);

// Create booking
var booking = await client.Bookings.CreateAsync(bookingRequest);
```

### JavaScript Client Library

```javascript
// npm install skirent-client
import { SkiRentalClient } from 'skirent-client';

const client = new SkiRentalClient('https://api.skirent.pl');

// Authentication
await client.auth.login('username', 'password');

// Equipment search
const equipment = await client.equipment.getAvailable('classic', 'skis');

// Create booking
const booking = await client.bookings.create(bookingRequest);
```

---

## Support and Documentation

### Getting Help

- **API Documentation**: This document
- **Code Examples**: GitHub repository examples folder
- **Support Email**: api-support@skirent.pl
- **Status Page**: https://status.skirent.pl

### Community

- **GitHub Issues**: Report bugs and request features
- **Developer Forum**: https://forum.skirent.pl
- **Discord Server**: Community support and discussions

### Training

- **API Workshops**: Monthly online workshops
- **Video Tutorials**: YouTube channel with step-by-step guides
- **Documentation Portal**: Comprehensive guides and best practices

---

This API documentation provides complete coverage of all public interfaces in the SkiRental system. For specific implementation details or additional examples, refer to the individual project documentation files.
