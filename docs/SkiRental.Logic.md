# SkiRental.Logic - Business Logic Layer Documentation

## Overview

The `SkiRental.Logic` project contains the application's business logic, services, repositories, and data access layer. It implements the core business rules and orchestrates operations between the domain models and external systems like payment providers and databases.

## Project Structure

```
SkiRental.Logic/
├── Interfaces/
│   └── IInterfaces.cs         # Repository and service contracts
├── Services/
│   ├── EquipmentMathService.cs    # Equipment recommendation algorithm
│   ├── BookingService.cs          # Rental lifecycle management
│   ├── PaymentService.cs          # Payment processing orchestration
│   └── AnalyticsService.cs        # Business intelligence calculations
├── Migrations/                    # EF Core database migrations
├── SkiRentalDbContext.cs          # Entity Framework DbContext
├── SkiRentalDbContextFactory.cs   # DbContext factory for design-time tools
├── ServiceCollectionExtensions.cs # DI registration extensions
└── SkiRental.Logic.csproj
```

## Core Interfaces

**File**: `Interfaces/IInterfaces.cs`

### IEquipmentRepository

Manages equipment data access operations.

```csharp
public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetAvailableByStyleAsync(
        SkiStyle style, EquipmentCategory category, CancellationToken ct = default);
    Task<Equipment> CreateAsync(Equipment equipment, CancellationToken ct = default);
    Task<Equipment> UpdateAsync(Equipment equipment, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

#### Methods

| Method | Purpose | Parameters | Returns |
|--------|---------|------------|---------|
| `GetByIdAsync` | Retrieve equipment by ID | `id`, `ct` | `Equipment?` |
| `GetAllAsync` | Get all equipment | `ct` | `IReadOnlyList<Equipment>` |
| `GetAvailableAsync` | Get available equipment only | `ct` | `IReadOnlyList<Equipment>` |
| `GetAvailableByStyleAsync` | Filter by style and category | `style`, `category`, `ct` | `IReadOnlyList<Equipment>` |
| `CreateAsync` | Add new equipment | `equipment`, `ct` | `Equipment` |
| `UpdateAsync` | Update existing equipment | `equipment`, `ct` | `Equipment` |
| `DeleteAsync` | Remove equipment | `id`, `ct` | `Task` |

### IRentalRepository

Manages rental transaction data access.

```csharp
public interface IRentalRepository
{
    Task<Rental?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetActiveRentalsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetOverdueRentalsAsync(CancellationToken ct = default);
    Task<Rental> CreateAsync(Rental rental, CancellationToken ct = default);
    Task<Rental> UpdateAsync(Rental rental, CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetRentalsByDateRangeAsync(
        DateTime start, DateTime end, CancellationToken ct = default);
}
```

#### Methods

| Method | Purpose | Parameters | Returns |
|--------|---------|------------|---------|
| `GetByIdAsync` | Retrieve rental by ID | `id`, `ct` | `Rental?` |
| `GetByCustomerIdAsync` | Get customer's rental history | `customerId`, `ct` | `IReadOnlyList<Rental>` |
| `GetActiveRentalsAsync` | Get currently active rentals | `ct` | `IReadOnlyList<Rental>` |
| `GetOverdueRentalsAsync` | Get overdue rentals | `ct` | `IReadOnlyList<Rental>` |
| `CreateAsync` | Create new rental | `rental`, `ct` | `Rental` |
| `UpdateAsync` | Update existing rental | `rental`, `ct` | `Rental` |
| `GetRentalsByDateRangeAsync` | Get rentals in date range | `start`, `end`, `ct` | `IReadOnlyList<Rental>` |

### IPaymentProvider

Payment processing abstraction for multiple payment gateways.

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

#### Payment Operations

| Method | Purpose | Use Case |
|--------|---------|----------|
| `InitiateBlikPayment` | Process BLIK mobile payment | Immediate payment |
| `PreauthorizePayment` | Hold funds for later capture | Rental deposits |
| `CapturePreauthorization` | Finalize pre-authorized payment | Rental completion |
| `RefundPayment` | Process refunds | Returns/cancellations |
| `CheckPaymentStatus` | Verify payment state | Status updates |

---

## Business Services

### EquipmentMathService

**File**: `Services/EquipmentMathService.cs`

Implements the "Intelligent Assistant" algorithm for equipment recommendations based on user physical data and skiing preferences.

#### Algorithm Overview

The service uses scientifically-based formulas combined with empirical corrections to recommend optimal equipment:

1. **Ski Length Calculation**
   - Classic Style: `Height + (20-30cm)` based on skill level
   - Skate Style: `Height + (10-15cm)` based on skill level
   - BMI Correction: `±5cm` for under/overweight users

2. **Pole Length Calculation**
   - Classic: `Height × 0.85`
   - Skate: `Height × 0.90`

3. **Stiffness (Flex Index)**
   - Base values by skill level (42-72)
   - BMI adjustments: `±5 points`

#### Constants

```csharp
private const decimal ClassicSkiOffsetMin = 20m;
private const decimal ClassicSkiOffsetMax = 30m;
private const decimal ClassicPoleMultiplier = 0.85m;
private const decimal SkateSkiOffsetMin = 10m;
private const decimal SkateSkiOffsetMax = 15m;
private const decimal SkatePoleMultiplier = 0.90m;
private const decimal BmiCorrectionCm = 5m;
private const decimal BmiNormalMin = 18.5m;
private const decimal BmiNormalMax = 24.9m;
```

#### Base Stiffness Mapping

| SkillLevel | BaseFlex | Description |
|------------|-----------|-------------|
| Beginner | 42 | Soft flex for learning |
| Intermediate | 52 | Medium flex for progress |
| Advanced | 62 | Stiff flex for performance |
| Expert | 72 | Very stiff for racing |

#### Public Methods

##### RecommendAsync

```csharp
public async Task<EquipmentRecommendation> RecommendAsync(
    EquipmentSelectionParameters parameters, 
    CancellationToken ct = default)
```

**Purpose**: Generate complete equipment recommendation with available inventory.

**Parameters**:
- `parameters`: User physical data and preferences
- `ct`: Cancellation token

**Returns**: `EquipmentRecommendation` with calculated values and matching equipment

**Process**:
1. Calculate optimal ski length, pole length, and stiffness
2. Apply BMI corrections if needed
3. Query available equipment from repository
4. Filter by size tolerance (±5cm)
5. Rank by closeness to calculated values
6. Return recommendation with explanations

##### CalculateSkiLength

```csharp
public decimal CalculateSkiLength(EquipmentSelectionParameters parameters)
```

**Formula**: 
```
baseOffset = interpolate(minOffset, maxOffset, skillLevel)
length = height + baseOffset + bmiCorrection
return roundToNearestHalf(length, 2.5m)
```

##### CalculatePoleLengthCm

```csharp
public decimal CalculatePoleLengthCm(EquipmentSelectionParameters parameters)
```

**Formula**:
```
multiplier = style == Classic ? 0.85m : 0.90m
length = height × multiplier
return roundToNearestHalf(length, 2.5m)
```

##### CalculateStiffness

```csharp
public int CalculateStiffness(EquipmentSelectionParameters parameters)
```

**Formula**:
```
baseStiffness = BaseStiffness[skillLevel]
bmiCorrection = overweight ? +5 : underweight ? -5 : 0
return clamp(baseStiffness + bmiCorrection, 30, 90)
```

#### Usage Example

```csharp
var parameters = new EquipmentSelectionParameters
{
    Height = 175,
    Weight = 78m,
    ShoeSize = 43,
    Style = SkiStyle.Classic,
    SkillLevel = SkillLevel.Intermediate
};

var recommendation = await equipmentMathService.RecommendAsync(parameters);

// Results:
// RecommendedSkiLength = 202.5 cm
// RecommendedPoleLength = 147.5 cm  
// RecommendedStiffness = 57
// BmiCorrectionApplied = true (BMI = 25.5, overweight)
```

---

### BookingService

**File**: `Services/BookingService.cs`

Orchestrates the complete rental lifecycle from creation to completion.

#### Core Responsibilities

1. **Rental Creation**: Validate and create new rentals
2. **Equipment Assignment**: Select and reserve equipment
3. **Payment Processing**: Coordinate with payment providers
4. **Status Management**: Handle rental state transitions
5. **QR Code Generation**: Create return verification codes

#### Key Methods

##### CreateBookingAsync

```csharp
public async Task<BookingResult> CreateBookingAsync(
    CreateBookingRequest request, 
    CancellationToken ct = default)
```

**Process**:
1. Validate user profile and equipment availability
2. Calculate total cost based on duration and equipment rates
3. Generate unique QR code for return verification
4. Create rental entity with proper status
5. Process payment (pre-authorization or immediate)
6. Update equipment status to 'Rented'
7. Return booking confirmation

##### ProcessReturnAsync

```csharp
public async Task<ReturnResult> ProcessReturnAsync(
    string qrCode, 
    ReturnConditionData conditionData,
    CancellationToken ct = default)
```

**Process**:
1. Validate QR code and locate rental
2. Check return time for overdue calculation
3. Update equipment condition ratings
4. Process final payment capture
5. Update equipment status to 'Available'
6. Generate return receipt

##### CancelBookingAsync

```csharp
public async Task<CancellationResult> CancelBookingAsync(
    int rentalId, 
    CancellationToken ct = default)
```

**Process**:
1. Verify cancellation eligibility
2. Process refund if payment was made
3. Release equipment back to inventory
4. Update rental status to 'Cancelled'
5. Send cancellation notification

#### Business Rules

- **Duration Limits**: Maximum 24 hours per rental
- **Deposit Requirements**: Pre-authorization for high-value equipment
- **Overdue Penalties**: Additional charges for late returns
- **Condition Tracking**: 1-5 scale for equipment condition
- **Cancellation Window**: Free cancellation up to 1 hour before start

---

### PaymentService

**File**: `Services/PaymentService.cs`

Manages payment processing workflow and integration with external payment providers.

#### Payment Workflow

1. **Pre-authorization**: Hold funds when rental starts
2. **Capture**: Finalize payment on rental completion
3. **Refund**: Process returns and cancellations
4. **Status Tracking**: Monitor payment states

#### Key Methods

##### ProcessRentalPaymentAsync

```csharp
public async Task<PaymentProcessingResult> ProcessRentalPaymentAsync(
    Rental rental, 
    PaymentMethod paymentMethod,
    CancellationToken ct = default)
```

**Strategies**:
- **BLIK**: Immediate payment for short rentals
- **Pre-authorization**: Hold amount for longer rentals
- **Multi-payment**: Split payments for expensive equipment

##### CapturePaymentAsync

```csharp
public async Task<PaymentCaptureResult> CapturePaymentAsync(
    string externalTransactionId, 
    decimal finalAmount,
    CancellationToken ct = default)
```

**Process**:
1. Verify pre-authorization exists
2. Calculate final amount (including overdue fees)
3. Capture payment from provider
4. Update payment records
5. Send receipt

##### ProcessRefundAsync

```csharp
public async Task<RefundResult> ProcessRefundAsync(
    int rentalId, 
    decimal? partialAmount = null,
    CancellationToken ct = default)
```

**Refund Types**:
- **Full Refund**: Complete rental cancellation
- **Partial Refund**: Equipment issues or service problems
- **Overage Refund**: Deposit return after equipment return

#### Payment Provider Integration

```csharp
public class PayUPaymentProvider : IPaymentProvider
{
    private readonly PayUOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayUPaymentProvider> _logger;

    // BLIK payment implementation
    public async Task<PaymentResult> InitiateBlikPayment(
        decimal amount, string blikCode, string orderId, string description)
    
    // Pre-authorization for rental deposits
    public async Task<PaymentResult> PreauthorizePayment(
        decimal amount, string orderId, string description)
    
    // Capture pre-authorized funds
    public async Task<PaymentResult> CapturePreauthorization(
        string externalTransactionId, decimal amount)
    
    // Refund processing
    public async Task<PaymentResult> RefundPayment(
        string externalTransactionId, decimal? partialAmount = null)
    
    // Status verification
    public async Task<PaymentResult> CheckPaymentStatus(string externalTransactionId)
}
```

---

### AnalyticsService

**File**: `Services/AnalyticsService.cs`

Provides business intelligence and utilization analytics for fleet management.

#### Key Metrics

##### Utilization Rate

```csharp
public async Task<UtilizationReport> CalculateUtilizationRateAsync(
    int equipmentId, 
    DateTime seasonStart, 
    DateTime seasonEnd,
    CancellationToken ct = default)
```

**Formula**:
```
UtilizationRate = Σ(RentalHours) / TotalSeasonHours
```

**Report Contents**:
- Total rented hours
- Season available hours  
- Utilization rate (0.0 - 1.0)
- Utilization percentage
- Revenue generated
- Maintenance downtime

##### Fleet Analytics

```csharp
public async Task<FleetAnalyticsReport> GetFleetAnalyticsAsync(
    DateTime startDate, 
    DateTime endDate,
    CancellationToken ct = default)
```

**Metrics**:
- **Overall Utilization**: Fleet-wide usage percentage
- **Revenue per Equipment**: Financial performance
- **Maintenance Frequency**: Service interval analysis
- **Popular Equipment**: Most rented items
- **Peak Usage Times**: Demand patterns

##### Customer Analytics

```csharp
public async Task<CustomerAnalyticsReport> GetCustomerAnalyticsAsync(
    DateTime startDate, 
    DateTime endDate,
    CancellationToken ct = default)
```

**Insights**:
- **Customer Retention**: Repeat rental rates
- **Average Rental Value**: Customer spending patterns
- **Skill Level Distribution**: User demographics
- **Style Preferences**: Classic vs Skate popularity

---

## Data Access Layer

### SkiRentalDbContext

**File**: `SkiRentalDbContext.cs`

Entity Framework Core context with optimized configurations.

#### Entity Configurations

##### Equipment Configuration

```csharp
builder.Entity<Equipment>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
    entity.HasIndex(e => e.SerialNumber).IsUnique();
    entity.Property(e => e.DailyRate).HasPrecision(8, 2);
    entity.Property(e => e.Size).HasPrecision(5, 1);
    entity.HasMany(e => e.RentalEquipments)
          .WithOne(re => re.Equipment)
          .HasForeignKey(re => re.EquipmentId);
});
```

##### Rental Configuration

```csharp
builder.Entity<Rental>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
    entity.Property(e => e.PreauthorizedAmount).HasPrecision(10, 2);
    entity.Property(e => e.QrCode).HasMaxLength(500);
    entity.HasIndex(e => e.QrCode).IsUnique();
    entity.HasOne(e => e.Customer)
          .WithMany(u => u.Rentals)
          .HasForeignKey(e => e.CustomerProfileId);
});
```

##### UserProfile Configuration

```csharp
builder.Entity<UserProfile>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Weight).HasPrecision(5, 1);
    entity.Property(e => e.Height).IsRequired();
    entity.Property(e => e.ShoeSize).IsRequired();
    entity.HasMany(e => e.Rentals)
          .WithOne(r => r.Customer)
          .HasForeignKey(r => r.CustomerProfileId);
});
```

#### Performance Optimizations

- **Query Optimization**: Eager loading for related data
- **Indexing Strategy**: Strategic indexes on frequently queried fields
- **Batch Operations**: Bulk updates for status changes
- **Connection Pooling**: Configured for high concurrency

---

## Dependency Injection Configuration

**File**: `ServiceCollectionExtensions.cs`

### Service Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSkiRentalLogic(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<SkiRentalDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        services.AddScoped<IRentalRepository, EfRentalRepository>();

        // Domain services
        services.AddScoped<EquipmentMathService>();
        services.AddScoped<BookingService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<AnalyticsService>();

        // Payment providers
        services.AddScoped<IPaymentProvider, PayUPaymentProvider>();

        return services;
    }
}
```

### Configuration Options

```csharp
public class PayUOptions
{
    public string PosId { get; set; } = string.Empty;
    public string SignatureKey { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
}
```

---

## Database Migrations

### Migration Strategy

Migrations are managed using EF Core tools with the following workflow:

```bash
# Add new migration
dotnet ef migrations add MigrationName --project SkiRental.Logic

# Update database
dotnet ef database update --project SkiRental.Logic

# Generate SQL script
dotnet ef migrations script --project SkiRental.Logic
```

### Current Migrations

1. **InitialCreate**: Base schema with all entities
2. **AddEquipmentMaintenance**: Added maintenance tracking
3. **AddPaymentEnhancements**: Enhanced payment processing
4. **AddQRCodeIndexing**: Performance optimization for QR lookups

---

## Error Handling and Logging

### Exception Strategy

```csharp
public class BusinessException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }
    
    public BusinessException(string errorCode, string message, 
        Dictionary<string, object>? context = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Context = context ?? new Dictionary<string, object>();
    }
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

### Logging Strategy

- **Structured Logging**: Using Serilog with contextual data
- **Performance Logging**: Operation timing and metrics
- **Error Logging**: Detailed exception information with context
- **Audit Logging**: Business operations tracking

---

## Testing Strategy

### Unit Tests

- **EquipmentMathService**: Algorithm validation with test cases
- **BookingService**: Business rule verification
- **PaymentService**: Payment workflow testing
- **AnalyticsService**: Calculation accuracy verification

### Integration Tests

- **Repository Implementations**: Database operations
- **Payment Providers**: External service integration
- **Service Orchestration**: End-to-end workflows

### Test Data Management

```csharp
public class TestDataBuilder
{
    public static Equipment CreateTestEquipment(
        EquipmentCategory category = EquipmentCategory.Skis)
    {
        return new Equipment
        {
            SerialNumber = $"TEST-{Guid.NewGuid()}",
            Brand = "TestBrand",
            Model = "TestModel",
            Category = category,
            Size = 180m,
            DailyRate = 50m,
            Status = EquipmentStatus.Available
        };
    }
}
```

---

## Performance Considerations

### Database Optimization

- **Query Optimization**: Proper indexing and query plans
- **Connection Management**: Efficient connection pooling
- **Caching Strategy**: Redis for frequently accessed data
- **Batch Operations**: Bulk updates for status changes

### Service Performance

- **Async Operations**: All I/O operations are asynchronous
- **Cancellation Tokens**: Proper cancellation support
- **Memory Management**: Efficient object lifecycle management
- **Parallel Processing**: Concurrent operations where safe

---

## Security Considerations

### Data Protection

- **PII Encryption**: Sensitive customer data encryption
- **Payment Security**: PCI compliance for payment data
- **Access Control**: Role-based data access
- **Audit Trail**: Complete operation logging

### Input Validation

- **Parameter Validation**: Comprehensive input checking
- **SQL Injection Prevention**: Parameterized queries
- **XSS Protection**: Output encoding in web contexts
- **Rate Limiting**: API endpoint protection

---

## External Dependencies

### NuGet Packages

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
```

### External Services

- **PayU API**: Payment processing
- **OpenWeatherMap**: Weather data (via app layer)
- **Email Service**: Notifications (future enhancement)

---

## Future Enhancements

### Planned Features

1. **Event Sourcing**: Domain events for audit trails
2. **CQRS Pattern**: Separate read/write models
3. **Message Queue**: Asynchronous processing
4. **Machine Learning**: Predictive analytics
5. **Multi-tenancy**: Support for multiple rental locations

### Scalability Improvements

1. **Microservices**: Service decomposition
2. **Database Sharding**: Horizontal scaling
3. **Load Balancing**: Service distribution
4. **Caching Layer**: Redis implementation
5. **API Gateway**: External API management

---

## Version History

- **v1.0**: Initial service implementation
- **v1.1**: Added payment processing
- **v1.2**: Enhanced analytics capabilities
- **v1.3**: Performance optimizations
- **v1.4**: Security enhancements
- **v2.0**: Architecture improvements (planned)
