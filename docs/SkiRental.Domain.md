# SkiRental.Domain - Domain Layer Documentation

## Overview

The `SkiRental.Domain` project contains the core domain models, enums, and business entities that form the foundation of the ski rental management system. This layer follows Domain-Driven Design (DDD) principles and contains no external dependencies.

## Project Structure

```
SkiRental.Domain/
├── Enums/
│   └── Enums.cs           # Domain enumerations
├── Models/
│   ├── Equipment.cs       # Equipment entity
│   ├── UserProfile.cs     # User profile entity
│   ├── Rental.cs          # Rental aggregate root
│   └── EquipmentRecommendation.cs # DTOs for recommendations
└── SkiRental.Domain.csproj
```

## Domain Models

### Equipment Model

**File**: `Models/Equipment.cs`

Represents a single piece of rental equipment in the system.

#### Properties

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `Id` | `int` | Primary key | Database-generated |
| `SerialNumber` | `string` | Unique manufacturer serial number | Required, Max 100 chars, Unique |
| `Brand` | `string` | Equipment manufacturer | Max 100 chars |
| `Model` | `string` | Equipment model name | Max 100 chars |
| `Category` | `EquipmentCategory` | Type of equipment | Enum (Skis, Poles, Boots, Gloves, Helmet) |
| `Size` | `decimal` | Size in cm or EU shoe size | Decimal(5,1) |
| `Stiffness` | `int?` | Flex index (1-120) | Optional |
| `Status` | `EquipmentStatus` | Current status | Enum (Available, Rented, Maintenance, Retired) |
| `CompatibleStyle` | `SkiStyle?` | Compatible skiing style | Optional (Classic, Skate, Combi) |
| `YearOfProduction` | `int` | Manufacturing year | Required |
| `DailyRate` | `decimal` | Daily rental price in PLN | Decimal(8,2) |
| `TotalRentalDays` | `int` | Total days rented (analytics) | Default 0 |
| `ImagePath` | `string?` | Path to equipment image | Optional, Max 500 chars |
| `LastMaintenanceDate` | `DateTime?` | Last service date | Optional |
| `MaintenanceNotes` | `string?` | Service notes | Optional, Max 1000 chars |

#### Navigation Properties

- `RentalEquipments` - Collection of rental equipment associations

#### Business Rules

- Serial numbers must be unique across all equipment
- Daily rates must be positive values
- Total rental days are automatically calculated by the system

---

### UserProfile Model

**File**: `Models/UserProfile.cs`

Contains customer personal data and physical measurements for equipment recommendations.

#### Properties

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `Id` | `int` | Primary key | Database-generated |
| `FirstName` | `string` | First name | Required, Max 100 chars |
| `LastName` | `string` | Last name | Required, Max 100 chars |
| `Email` | `string?` | Email address | Optional, Max 200 chars |
| `PhoneNumber` | `string?` | Phone number | Optional, Max 20 chars |
| `Height` | `int` | Height in centimeters | Range 100-250 |
| `Weight` | `decimal` | Weight in kilograms | Decimal(5,1), Range 30-300 |
| `ShoeSize` | `int` | EU shoe size | Range 30-55 |
| `SkillLevel` | `SkillLevel` | Skiing proficiency | Enum (Beginner, Intermediate, Advanced, Expert) |
| `PreferredStyle` | `SkiStyle` | Preferred skiing style | Enum (Classic, Skate, Combi) |
| `DateOfBirth` | `DateTime?` | Birth date | Optional |
| `Address` | `string?` | Full address | Optional, Max 500 chars |
| `City` | `string?` | City name | Optional, Max 100 chars |
| `ProfileImagePath` | `string?` | Profile photo path | Optional, Max 500 chars |
| `FavoriteRoutes` | `string?` | Preferred ski routes | Optional, Max 1000 chars |
| `PreferenceNotes` | `string?` | Customer preference notes | Optional, Max 2000 chars |
| `EmailNotificationsEnabled` | `bool` | Email notification preference | Default true |
| `SmsNotificationsEnabled` | `bool` | SMS notification preference | Default false |
| `CreatedAt` | `DateTime` | Account creation date | UTC |
| `UpdatedAt` | `DateTime?` | Last update date | Optional |

#### Computed Properties (NotMapped)

| Property | Type | Description | Formula |
|----------|------|-------------|---------|
| `Bmi` | `decimal` | Body Mass Index | `Weight / (Height/100)²` |
| `IsBmiNormal` | `bool` | BMI in normal range | `18.5 ≤ BMI ≤ 24.9` |
| `IsUnderweight` | `bool` | BMI below normal | `BMI < 18.5` |
| `IsOverweight` | `bool` | BMI above normal | `BMI > 24.9` |
| `Age` | `int?` | Calculated age | Current year - birth year |
| `TotalRentalsCount` | `int` | Number of rentals | Count of Rentals collection |
| `LastRentalDate` | `DateTime?` | Most recent rental | Max of Rental.StartTimes |
| `TotalSpent` | `decimal` | Total money spent | Sum of paid rental amounts |
| `FavoriteEquipmentCategory` | `EquipmentCategory?` | Most rented category | Mode of rented equipment categories |

#### Navigation Properties

- `Rentals` - Collection of user's rental history

#### Business Rules

- Height must be between 100-250 cm for realistic adult users
- Weight range supports 30-300 kg for comprehensive coverage
- Shoe sizes follow EU standard (30-55)
- BMI calculations use standard medical formulas
- Age calculation accounts for birth date timing

---

### Rental Model

**File**: `Models/Rental.cs`

The main aggregate root representing a rental transaction.

#### Properties

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `Id` | `int` | Primary key | Database-generated |
| `CustomerProfileId` | `int` | Foreign key to UserProfile | Required |
| `EmployeeId` | `int?` | Employee handling rental | Optional |
| `StartTime` | `DateTime` | Rental start time | Required |
| `EndTime` | `DateTime` | Scheduled end time | Required |
| `ActualReturnTime` | `DateTime?` | Actual return time | Optional |
| `QrCode` | `string?` | QR code for return verification | Optional, Max 500 chars |
| `NfcTagData` | `string?` | NFC tag data (Base64) | Optional, Max 1000 chars |
| `IsPreauthorized` | `bool` | Payment pre-authorized | Default false |
| `PaymentStatus` | `PaymentStatus` | Payment state | Enum (Unpaid, Preauthorized, Paid, Refunded, Failed) |
| `TotalAmount` | `decimal` | Total rental cost | Decimal(10,2) |
| `PreauthorizedAmount` | `decimal?` | Pre-authorized amount | Optional, Decimal(10,2) |
| `PayUOrderId` | `string?` | PayU transaction ID | Optional, Max 200 chars |
| `Status` | `RentalStatus` | Rental status | Enum (Pending, Active, Completed, Cancelled, Overdue) |
| `Notes` | `string?` | Rental notes | Optional, Max 500 chars |
| `CreatedAt` | `DateTime` | Creation timestamp | UTC |

#### Computed Properties (NotMapped)

| Property | Type | Description | Formula |
|----------|------|-------------|---------|
| `PlannedDurationHours` | `double` | Planned rental duration | `(EndTime - StartTime).TotalHours` |

#### Navigation Properties

- `Customer` - Related UserProfile
- `RentalEquipments` - Collection of equipment items in this rental
- `Payments` - Collection of payment transactions

#### Business Rules

- EndTime must be after StartTime
- TotalAmount must be non-negative
- QR codes must be unique when present
- Status transitions follow business workflow

---

### Supporting Models

#### RentalEquipment (Join Table)

**File**: `Models/Rental.cs` (nested class)

Many-to-many relationship between Rental and Equipment.

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `RentalId` | `int` | Foreign key to Rental | Composite PK |
| `EquipmentId` | `int` | Foreign key to Equipment | Composite PK |
| `ConditionAtPickup` | `int` | Equipment condition (1-5) | Default 5 |
| `ConditionAtReturn` | `int?` | Return condition (1-5) | Optional |

#### Payment Model

**File**: `Models/Rental.cs` (nested class)

Records payment transactions.

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `Id` | `int` | Primary key | Database-generated |
| `RentalId` | `int` | Foreign key to Rental | Required |
| `Amount` | `decimal` | Payment amount | Decimal(10,2) |
| `Status` | `PaymentStatus` | Payment status | Enum |
| `ExternalTransactionId` | `string?` | External payment ID | Optional, Max 200 chars |
| `PaymentMethod` | `string?` | Payment method used | Optional, Max 50 chars |
| `CreatedAt` | `DateTime` | Creation timestamp | UTC |
| `ProcessedAt` | `DateTime?` | Processing timestamp | Optional |
| `ProviderResponse` | `string?` | Payment provider response | Optional, Max 1000 chars |

---

## Domain Enums

**File**: `Enums/Enums.cs`

### SkillLevel

| Value | Description | Use Case |
|-------|-------------|----------|
| `Beginner` | Novice skier | Equipment recommendations, pricing |
| `Intermediate` | Moderate experience | Stiffness calculations |
| `Advanced` | Experienced skier | Advanced equipment selection |
| `Expert` | Professional level | High-performance equipment |

### SkiStyle

| Value | Description | Equipment Impact |
|-------|-------------|------------------|
| `Classic` | Traditional parallel skiing | Longer skis, shorter poles |
| `Skate` | Skate skiing technique | Shorter skis, longer poles |
| `Combi` | Universal combination | Versatile equipment |

### EquipmentStatus

| Value | Description | Business Impact |
|-------|-------------|-----------------|
| `Available` | Ready for rental | Visible in search results |
| `Rented` | Currently rented | Hidden from availability |
| `Maintenance` | Under service | Not available for rental |
| `Retired` | Decommissioned | Removed from active fleet |

### EquipmentCategory

| Value | Description | Rental Logic |
|-------|-------------|--------------|
| `Skis` | Cross-country skis | Size in cm, stiffness important |
| `Poles` | Ski poles | Length based on height |
| `Boots` | Ski boots | Size in EU |
| `Gloves` | Ski gloves | Size-based selection |
| `Helmet` | Safety helmet | Size-based selection |

### RentalStatus

| Value | Description | Workflow Position |
|-------|-------------|-------------------|
| `Pending` | Awaiting confirmation | Initial state |
| `Active` | Currently in progress | Equipment issued |
| `Completed` | Successfully returned | Final state |
| `Cancelled` | Cancelled before start | Aborted workflow |
| `Overdue` | Past return time | Requires attention |

### PaymentStatus

| Value | Description | Financial Impact |
|-------|-------------|-----------------|
| `Unpaid` | Payment pending | Liability exists |
| `Preauthorized` | Amount held | Temporary hold |
| `Paid` | Payment completed | Revenue recognized |
| `Refunded` | Money returned | Revenue reversal |
| `Failed` | Payment error | Requires resolution |

---

## Business Rules and Validation

### Equipment Sizing Rules

1. **Ski Length Calculation**:
   - Classic: Height + 20-30cm (based on skill level)
   - Skate: Height + 10-15cm (based on skill level)
   - BMI correction: ±5cm for under/overweight users

2. **Pole Length Calculation**:
   - Classic: Height × 0.85
   - Skate: Height × 0.90

3. **Stiffness Selection**:
   - Beginner: Flex Index 42
   - Intermediate: Flex Index 52
   - Advanced: Flex Index 62
   - Expert: Flex Index 72
   - BMI adjustment: ±5 points

### Rental Workflow

1. **Creation**: Rental starts in `Pending` status
2. **Activation**: Equipment issued → `Active` status
3. **Completion**: Equipment returned → `Completed` status
4. **Exceptions**: Cancellation or overdue handling

### Payment Processing

1. **Pre-authorization**: Hold amount during rental
2. **Capture**: Final payment on completion
3. **Refund**: Process returns and cancellations
4. **Failure Handling**: Retry and error management

---

## Data Annotations and Validation

### Validation Rules

- **Required Fields**: All essential business fields are marked with `[Required]`
- **String Lengths**: Max lengths enforced on all string properties
- **Value Ranges**: Numerical constraints on measurements
- **Regular Expressions**: Email format validation
- **Custom Validation**: BMI calculations and business rules

### Database Mapping

- **Table Names**: Explicit `[Table]` attributes for clarity
- **Column Types**: Precise `decimal` types for financial data
- **Indexes**: Unique constraints on serial numbers and QR codes
- **Relationships**: Proper foreign key configurations

---

## Usage Examples

### Creating a New User Profile

```csharp
var user = new UserProfile
{
    FirstName = "Jan",
    LastName = "Kowalski",
    Height = 175,
    Weight = 75m,
    ShoeSize = 43,
    SkillLevel = SkillLevel.Intermediate,
    PreferredStyle = SkiStyle.Classic,
    Email = "jan.kowalski@example.com"
};

// BMI automatically calculated: 24.5 (normal)
// IsBmiNormal = true
```

### Equipment Recommendation Logic

```csharp
// For 175cm, 75kg intermediate user:
// Classic skis: 175 + 25cm = 200cm
// Classic poles: 175 × 0.85 = 148.75cm → 150cm
// Stiffness: 52 (intermediate)
```

### Rental Creation

```csharp
var rental = new Rental
{
    CustomerProfileId = userId,
    StartTime = DateTime.Now,
    EndTime = DateTime.Now.AddHours(4),
    TotalAmount = 120m,
    Status = RentalStatus.Pending
};
```

---

## Dependencies

This project has no external dependencies and is designed to be completely self-contained. It can be referenced by:
- Application services layer
- Test projects
- Other domain modules

## Future Enhancements

1. **Value Objects**: Extract complex concepts like Money, Measurement
2. **Domain Events**: Add event sourcing for audit trails
3. **Specifications**: Complex query encapsulation
4. **Aggregates**: Strengthen aggregate boundaries
5. **Domain Services**: Complex business logic extraction

---

## Version History

- **v1.0**: Initial domain model implementation
- **v1.1**: Added equipment recommendation DTOs
- **v1.2**: Enhanced validation and business rules
- **v1.3**: Added authentication support fields
