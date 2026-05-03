# Database Schema and Entity Framework Core Documentation

## Overview

The SkiRental application uses Entity Framework Core with SQLite as the primary database provider. The database schema is designed to support the complete ski rental management system, including equipment inventory, customer profiles, rental transactions, and payment processing.

## Database Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Database Architecture                    │
├─────────────────────────────────────────────────────────────────┤
│  Application Layer                                               │
│  ├── SkiRentalDbContext (EF Core)                              │
│  ├── Repository Pattern                                         │
│  └── Unit of Work Pattern                                       │
├─────────────────────────────────────────────────────────────────┤
│  Entity Framework Core                                           │
│  ├── Model Mapping                                             │
│  ├── Query Optimization                                         │
│  ├── Change Tracking                                            │
│  └── Migrations                                                 │
├─────────────────────────────────────────────────────────────────┤
│  Database Provider                                               │
│  ├── SQLite (Development/Mobile)                                │
│  ├── SQL Server (Production)                                   │
│  └── In-Memory (Testing)                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Entity Relationships

### Entity Relationship Diagram

```
UserProfile (1) ────── (n) Rental
    │                         │
    │                         │
    │                         ├─ (n) RentalEquipment (n) ── Equipment (1)
    │                         │
    │                         └─ (n) Payment
    │
    └─ (1) CustomerProfile
```

### Primary Entities

| Entity | Purpose | Key Relationships |
|--------|---------|-------------------|
| **UserProfile** | Customer information and preferences | Rentals (1:n) |
| **Equipment** | Equipment inventory and specifications | RentalEquipment (1:n) |
| **Rental** | Rental transaction records | UserProfile (n:1), RentalEquipment (1:n), Payment (1:n) |
| **RentalEquipment** | Many-to-many relationship between rentals and equipment | Rental (n:1), Equipment (n:1) |
| **Payment** | Payment transaction records | Rental (n:1) |

---

## Database Schema

### UserProfile Table

```sql
CREATE TABLE UserProfiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT,
    PhoneNumber TEXT,
    Height INTEGER NOT NULL CHECK (Height >= 100 AND Height <= 250),
    Weight REAL NOT NULL CHECK (Weight >= 30.0 AND Weight <= 300.0),
    ShoeSize INTEGER NOT NULL CHECK (ShoeSize >= 30 AND ShoeSize <= 55),
    SkillLevel INTEGER NOT NULL, -- 0=Beginner, 1=Intermediate, 2=Advanced, 3=Expert
    PreferredStyle INTEGER NOT NULL, -- 0=Classic, 1=Skate, 2=Combi
    DateOfBirth TEXT, -- SQLite stores as TEXT, parsed as DateTime
    Address TEXT,
    City TEXT,
    ProfileImagePath TEXT,
    FavoriteRoutes TEXT,
    PreferenceNotes TEXT,
    EmailNotificationsEnabled INTEGER NOT NULL DEFAULT 1,
    SmsNotificationsEnabled INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL, -- UTC timestamp
    UpdatedAt TEXT
);

-- Indexes for performance
CREATE INDEX IX_UserProfiles_Email ON UserProfiles(Email);
CREATE INDEX IX_UserProfiles_City ON UserProfiles(City);
CREATE INDEX IX_UserProfiles_SkillLevel ON UserProfiles(SkillLevel);
CREATE INDEX IX_UserProfiles_PreferredStyle ON UserProfiles(PreferredStyle);
```

#### Constraints and Validation

- **Height**: Must be between 100-250 cm (realistic adult range)
- **Weight**: Must be between 30-300 kg (comprehensive coverage)
- **ShoeSize**: EU standard 30-55
- **SkillLevel**: Enum constraint (0-3)
- **PreferredStyle**: Enum constraint (0-2)
- **CreatedAt**: Required UTC timestamp
- **Email**: Unique index for efficient lookups

---

### Equipment Table

```sql
CREATE TABLE Equipment (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SerialNumber TEXT NOT NULL UNIQUE,
    Brand TEXT NOT NULL,
    Model TEXT NOT NULL,
    Category INTEGER NOT NULL, -- 0=Skis, 1=Poles, 2=Boots, 3=Gloves, 4=Helmet
    Size REAL NOT NULL CHECK (Size > 0),
    Stiffness INTEGER CHECK (Stiffness >= 1 AND Stiffness <= 120),
    Status INTEGER NOT NULL DEFAULT 0, -- 0=Available, 1=Rented, 2=Maintenance, 3=Retired
    CompatibleStyle INTEGER, -- NULL=Universal, 0=Classic, 1=Skate, 2=Combi
    YearOfProduction INTEGER NOT NULL,
    DailyRate REAL NOT NULL CHECK (DailyRate > 0),
    TotalRentalDays INTEGER NOT NULL DEFAULT 0,
    ImagePath TEXT,
    LastMaintenanceDate TEXT,
    MaintenanceNotes TEXT
);

-- Indexes for performance
CREATE UNIQUE INDEX IX_Equipment_SerialNumber ON Equipment(SerialNumber);
CREATE INDEX IX_Equipment_Category ON Equipment(Category);
CREATE INDEX IX_Equipment_Status ON Equipment(Status);
CREATE INDEX IX_Equipment_Size ON Equipment(Size);
CREATE INDEX IX_Equipment_Brand ON Equipment(Brand);
```

#### Constraints and Validation

- **SerialNumber**: Unique identifier for equipment tracking
- **Size**: Positive decimal for various measurement units
- **Stiffness**: Optional flex index (1-120) for skis only
- **Status**: Required status for availability tracking
- **DailyRate**: Positive decimal for pricing
- **YearOfProduction**: Required for equipment age tracking

---

### Rental Table

```sql
CREATE TABLE Rentals (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerProfileId INTEGER NOT NULL,
    EmployeeId INTEGER,
    StartTime TEXT NOT NULL, -- UTC timestamp
    EndTime TEXT NOT NULL, -- UTC timestamp
    ActualReturnTime TEXT, -- UTC timestamp
    QrCode TEXT UNIQUE,
    NfcTagData TEXT,
    IsPreauthorized INTEGER NOT NULL DEFAULT 0,
    PaymentStatus INTEGER NOT NULL DEFAULT 0, -- 0=Unpaid, 1=Preauthorized, 2=Paid, 3=Refunded, 4=Failed
    TotalAmount REAL NOT NULL CHECK (TotalAmount >= 0),
    PreauthorizedAmount REAL CHECK (PreauthorizedAmount >= 0),
    PayUOrderId TEXT,
    Status INTEGER NOT NULL DEFAULT 0, -- 0=Pending, 1=Active, 2=Completed, 3=Cancelled, 4=Overdue
    Notes TEXT,
    CreatedAt TEXT NOT NULL, -- UTC timestamp
    FOREIGN KEY (CustomerProfileId) REFERENCES UserProfiles(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE UNIQUE INDEX IX_Rentals_QrCode ON Rentals(QrCode);
CREATE INDEX IX_Rentals_CustomerProfileId ON Rentals(CustomerProfileId);
CREATE INDEX IX_Rentals_Status ON Rentals(Status);
CREATE INDEX IX_Rentals_StartTime ON Rentals(StartTime);
CREATE INDEX IX_Rentals_EndTime ON Rentals(EndTime);
CREATE INDEX IX_Rentals_PaymentStatus ON Rentals(PaymentStatus);
CREATE INDEX IX_Rentals_PayUOrderId ON Rentals(PayUOrderId);
```

#### Constraints and Validation

- **CustomerProfileId**: Foreign key with cascade delete
- **StartTime/EndTime**: Required timestamps with EndTime > StartTime
- **QrCode**: Unique identifier for return verification
- **TotalAmount**: Non-negative financial amount
- **Status**: Required status for workflow tracking
- **PaymentStatus**: Required status for payment tracking

---

### RentalEquipment Table (Join Table)

```sql
CREATE TABLE RentalEquipments (
    RentalId INTEGER NOT NULL,
    EquipmentId INTEGER NOT NULL,
    ConditionAtPickup INTEGER NOT NULL DEFAULT 5 CHECK (ConditionAtPickup >= 1 AND ConditionAtPickup <= 5),
    ConditionAtReturn INTEGER CHECK (ConditionAtReturn >= 1 AND ConditionAtReturn <= 5),
    PRIMARY KEY (RentalId, EquipmentId),
    FOREIGN KEY (RentalId) REFERENCES Rentals(Id) ON DELETE CASCADE,
    FOREIGN KEY (EquipmentId) REFERENCES Equipment(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IX_RentalEquipments_EquipmentId ON RentalEquipments(EquipmentId);
CREATE INDEX IX_RentalEquipments_ConditionAtPickup ON RentalEquipments(ConditionAtPickup);
CREATE INDEX IX_RentalEquipments_ConditionAtReturn ON RentalEquipments(ConditionAtReturn);
```

#### Constraints and Validation

- **Composite Primary Key**: Ensures unique equipment per rental
- **Condition Ratings**: 1-5 scale for equipment condition tracking
- **Cascade Deletes**: Maintains referential integrity

---

### Payment Table

```sql
CREATE TABLE Payments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RentalId INTEGER NOT NULL,
    Amount REAL NOT NULL CHECK (Amount >= 0),
    Status INTEGER NOT NULL, -- 0=Unpaid, 1=Preauthorized, 2=Paid, 3=Refunded, 4=Failed
    ExternalTransactionId TEXT,
    PaymentMethod TEXT,
    CreatedAt TEXT NOT NULL, -- UTC timestamp
    ProcessedAt TEXT, -- UTC timestamp
    ProviderResponse TEXT,
    FOREIGN KEY (RentalId) REFERENCES Rentals(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IX_Payments_RentalId ON Payments(RentalId);
CREATE INDEX IX_Payments_Status ON Payments(Status);
CREATE INDEX IX_Payments_ExternalTransactionId ON Payments(ExternalTransactionId);
CREATE INDEX IX_Payments_CreatedAt ON Payments(CreatedAt);
```

#### Constraints and Validation

- **Amount**: Non-negative financial amount
- **Status**: Required status for payment tracking
- **ExternalTransactionId**: Optional payment provider reference
- **CreatedAt/ProcessedAt**: Timestamps for payment lifecycle

---

## Entity Framework Core Configuration

### SkiRentalDbContext

**File**: `SkiRental.Logic\SkiRentalDbContext.cs`

The main database context with entity configurations.

#### Context Configuration

```csharp
public class SkiRentalDbContext : DbContext
{
    public SkiRentalDbContext(DbContextOptions<SkiRentalDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<Rental> Rentals { get; set; }
    public DbSet<RentalEquipment> RentalEquipments { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new RentalConfiguration());
        modelBuilder.ApplyConfiguration(new RentalEquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        
        // Seed data (for development)
        SeedData(modelBuilder);
    }
}
```

### Entity Configurations

#### UserProfile Configuration

```csharp
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Table configuration
        builder.ToTable("UserProfiles");
        
        // Primary key
        builder.HasKey(u => u.Id);
        
        // Property configurations
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.Email)
            .HasMaxLength(200);
        
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);
        
        builder.Property(u => u.Height)
            .IsRequired();
        
        builder.Property(u => u.Weight)
            .HasPrecision(5, 1)
            .IsRequired();
        
        builder.Property(u => u.ShoeSize)
            .IsRequired();
        
        builder.Property(u => u.SkillLevel)
            .IsRequired();
        
        builder.Property(u => u.PreferredStyle)
            .IsRequired();
        
        builder.Property(u => u.Address)
            .HasMaxLength(500);
        
        builder.Property(u => u.City)
            .HasMaxLength(100);
        
        builder.Property(u => u.ProfileImagePath)
            .HasMaxLength(500);
        
        builder.Property(u => u.FavoriteRoutes)
            .HasMaxLength(1000);
        
        builder.Property(u => u.PreferenceNotes)
            .HasMaxLength(2000);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime().ToString("O"),
                v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        
        builder.Property(u => u.UpdatedAt)
            .HasConversion(
                v => v?.ToUniversalTime().ToString("O"),
                v => v != null ? DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : null);
        
        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_UserProfiles_Email");
        
        builder.HasIndex(u => u.City)
            .HasDatabaseName("IX_UserProfiles_City");
        
        builder.HasIndex(u => u.SkillLevel)
            .HasDatabaseName("IX_UserProfiles_SkillLevel");
        
        builder.HasIndex(u => u.PreferredStyle)
            .HasDatabaseName("IX_UserProfiles_PreferredStyle");
        
        // Relationships
        builder.HasMany(u => u.Rentals)
            .WithOne(r => r.Customer)
            .HasForeignKey(r => r.CustomerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Computed properties (NotMapped)
        builder.Ignore(u => u.Bmi);
        builder.Ignore(u => u.IsBmiNormal);
        builder.Ignore(u => u.IsUnderweight);
        builder.Ignore(u => u.IsOverweight);
        builder.Ignore(u => u.Age);
        builder.Ignore(u => u.TotalRentalsCount);
        builder.Ignore(u => u.LastRentalDate);
        builder.Ignore(u => u.TotalSpent);
        builder.Ignore(u => u.FavoriteEquipmentCategory);
    }
}
```

#### Equipment Configuration

```csharp
public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        // Table configuration
        builder.ToTable("Equipment");
        
        // Primary key
        builder.HasKey(e => e.Id);
        
        // Property configurations
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(e => e.SerialNumber)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.Brand)
            .HasMaxLength(100);
        
        builder.Property(e => e.Model)
            .HasMaxLength(100);
        
        builder.Property(e => e.Size)
            .HasPrecision(5, 1)
            .IsRequired();
        
        builder.Property(e => e.DailyRate)
            .HasPrecision(8, 2)
            .IsRequired();
        
        builder.Property(e => e.ImagePath)
            .HasMaxLength(500);
        
        builder.Property(e => e.MaintenanceNotes)
            .HasMaxLength(1000);
        
        builder.Property(e => e.LastMaintenanceDate)
            .HasConversion(
                v => v?.ToUniversalTime().ToString("O"),
                v => v != null ? DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : null);
        
        // Indexes
        builder.HasIndex(e => e.SerialNumber)
            .IsUnique()
            .HasDatabaseName("IX_Equipment_SerialNumber");
        
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_Equipment_Category");
        
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Equipment_Status");
        
        builder.HasIndex(e => e.Size)
            .HasDatabaseName("IX_Equipment_Size");
        
        builder.HasIndex(e => e.Brand)
            .HasDatabaseName("IX_Equipment_Brand");
        
        // Relationships
        builder.HasMany(e => e.RentalEquipments)
            .WithOne(re => re.Equipment)
            .HasForeignKey(re => re.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### Rental Configuration

```csharp
public class RentalConfiguration : IEntityTypeConfiguration<Rental>
{
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        // Table configuration
        builder.ToTable("Rentals");
        
        // Primary key
        builder.HasKey(r => r.Id);
        
        // Property configurations
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(r => r.TotalAmount)
            .HasPrecision(10, 2)
            .IsRequired();
        
        builder.Property(r => r.PreauthorizedAmount)
            .HasPrecision(10, 2);
        
        builder.Property(r => r.QrCode)
            .HasMaxLength(500);
        
        builder.Property(r => r.NfcTagData)
            .HasMaxLength(1000);
        
        builder.Property(r => r.PayUOrderId)
            .HasMaxLength(200);
        
        builder.Property(r => r.Notes)
            .HasMaxLength(500);
        
        builder.Property(r => r.StartTime)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime().ToString("O"),
                v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        
        builder.Property(r => r.EndTime)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime().ToString("O"),
                v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        
        builder.Property(r => r.ActualReturnTime)
            .HasConversion(
                v => v?.ToUniversalTime().ToString("O"),
                v => v != null ? DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : null);
        
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime().ToString("O"),
                v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        
        // Indexes
        builder.HasIndex(r => r.QrCode)
            .IsUnique()
            .HasDatabaseName("IX_Rentals_QrCode");
        
        builder.HasIndex(r => r.CustomerProfileId)
            .HasDatabaseName("IX_Rentals_CustomerProfileId");
        
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Rentals_Status");
        
        builder.HasIndex(r => r.StartTime)
            .HasDatabaseName("IX_Rentals_StartTime");
        
        builder.HasIndex(r => r.EndTime)
            .HasDatabaseName("IX_Rentals_EndTime");
        
        builder.HasIndex(r => r.PaymentStatus)
            .HasDatabaseName("IX_Rentals_PaymentStatus");
        
        builder.HasIndex(r => r.PayUOrderId)
            .HasDatabaseName("IX_Rentals_PayUOrderId");
        
        // Relationships
        builder.HasOne(r => r.Customer)
            .WithMany(u => u.Rentals)
            .HasForeignKey(r => r.CustomerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(r => r.RentalEquipments)
            .WithOne(re => re.Rental)
            .HasForeignKey(re => re.RentalId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(r => r.Payments)
            .WithOne(p => p.Rental)
            .HasForeignKey(p => p.RentalId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Computed properties (NotMapped)
        builder.Ignore(r => r.PlannedDurationHours);
    }
}
```

#### RentalEquipment Configuration

```csharp
public class RentalEquipmentConfiguration : IEntityTypeConfiguration<RentalEquipment>
{
    public void Configure(EntityTypeBuilder<RentalEquipment> builder)
    {
        // Table configuration
        builder.ToTable("RentalEquipments");
        
        // Composite primary key
        builder.HasKey(re => new { re.RentalId, re.EquipmentId });
        
        // Property configurations
        builder.Property(re => re.ConditionAtPickup)
            .IsRequired();
        
        builder.Property(re => re.ConditionAtReturn);
        
        // Indexes
        builder.HasIndex(re => re.EquipmentId)
            .HasDatabaseName("IX_RentalEquipments_EquipmentId");
        
        builder.HasIndex(re => re.ConditionAtPickup)
            .HasDatabaseName("IX_RentalEquipments_ConditionAtPickup");
        
        builder.HasIndex(re => re.ConditionAtReturn)
            .HasDatabaseName("IX_RentalEquipments_ConditionAtReturn");
        
        // Relationships
        builder.HasOne(re => re.Rental)
            .WithMany(r => r.RentalEquipments)
            .HasForeignKey(re => re.RentalId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(re => re.Equipment)
            .WithMany(e => e.RentalEquipments)
            .HasForeignKey(re => re.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### Payment Configuration

```csharp
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Table configuration
        builder.ToTable("Payments");
        
        // Primary key
        builder.HasKey(p => p.Id);
        
        // Property configurations
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(p => p.Amount)
            .HasPrecision(10, 2)
            .IsRequired();
        
        builder.Property(p => p.ExternalTransactionId)
            .HasMaxLength(200);
        
        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(50);
        
        builder.Property(p => p.ProviderResponse)
            .HasMaxLength(1000);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime().ToString("O"),
                v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        
        builder.Property(p => p.ProcessedAt)
            .HasConversion(
                v => v?.ToUniversalTime().ToString("O"),
                v => v != null ? DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : null);
        
        // Indexes
        builder.HasIndex(p => p.RentalId)
            .HasDatabaseName("IX_Payments_RentalId");
        
        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payments_Status");
        
        builder.HasIndex(p => p.ExternalTransactionId)
            .HasDatabaseName("IX_Payments_ExternalTransactionId");
        
        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");
        
        // Relationships
        builder.HasOne(p => p.Rental)
            .WithMany(r => r.Payments)
            .HasForeignKey(p => p.RentalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## Database Migrations

### Migration Strategy

Migrations are managed using EF Core tools with automatic database schema updates.

### Current Migrations

#### 20240101000000_InitialCreate

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create all tables with initial schema
        migrationBuilder.CreateTable(
            name: "UserProfiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FirstName = table.Column<string>(type: "TEXT", nullable: false),
                LastName = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                Height = table.Column<int>(type: "INTEGER", nullable: false),
                Weight = table.Column<decimal>(type: "REAL", nullable: false),
                ShoeSize = table.Column<int>(type: "INTEGER", nullable: false),
                SkillLevel = table.Column<int>(type: "INTEGER", nullable: false),
                PreferredStyle = table.Column<int>(type: "INTEGER", nullable: false),
                DateOfBirth = table.Column<string>(type: "TEXT", nullable: true),
                Address = table.Column<string>(type: "TEXT", nullable: true),
                City = table.Column<string>(type: "TEXT", nullable: true),
                ProfileImagePath = table.Column<string>(type: "TEXT", nullable: true),
                FavoriteRoutes = table.Column<string>(type: "TEXT", nullable: true),
                PreferenceNotes = table.Column<string>(type: "TEXT", nullable: true),
                EmailNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                SmsNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserProfiles", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_UserProfiles_Email",
            table: "UserProfiles",
            column: "Email",
            unique: true);

        // Similar table creation for Equipment, Rentals, RentalEquipments, Payments
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "RentalEquipments");

        migrationBuilder.DropTable(
            name: "Rentals");

        migrationBuilder.DropTable(
            name: "Equipment");

        migrationBuilder.DropTable(
            name: "UserProfiles");
    }
}
```

#### 20240102000000_AddEquipmentMaintenance

```csharp
public partial class AddEquipmentMaintenance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LastMaintenanceDate",
            table: "Equipment",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MaintenanceNotes",
            table: "Equipment",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LastMaintenanceDate",
            table: "Equipment");

        migrationBuilder.DropColumn(
            name: "MaintenanceNotes",
            table: "Equipment");
    }
}
```

#### 20240103000000_AddPaymentEnhancements

```csharp
public partial class AddPaymentEnhancements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "PreauthorizedAmount",
            table: "Rentals",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsPreauthorized",
            table: "Rentals",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "PayUOrderId",
            table: "Rentals",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RentalId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "REAL", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ExternalTransactionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                ProcessedAt = table.Column<string>(type: "TEXT", nullable: true),
                ProviderResponse = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_Rentals_RentalId",
                    column: x => x.RentalId,
                    principalTable: "Rentals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
}
```

---

## Database Seeding

### Seed Data Configuration

```csharp
public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        // Seed equipment
        SeedEquipment(modelBuilder);
        
        // Seed users (for development)
        SeedUsers(modelBuilder);
    }

    private static void SeedEquipment(ModelBuilder modelBuilder)
    {
        var equipment = new[]
        {
            new Equipment
            {
                Id = 1,
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
            },
            new Equipment
            {
                Id = 2,
                SerialNumber = "FIS-2024-002",
                Brand = "Fischer",
                Model = "Carbonlite",
                Category = EquipmentCategory.Skis,
                Size = 190m,
                Stiffness = 57,
                Status = EquipmentStatus.Available,
                CompatibleStyle = SkiStyle.Skate,
                YearOfProduction = 2024,
                DailyRate = 65m
            },
            // Add more equipment as needed
        };

        modelBuilder.Entity<Equipment>().HasData(equipment);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        var users = new[]
        {
            new UserProfile
            {
                Id = 1,
                FirstName = "Jan",
                LastName = "Kowalski",
                Email = "jan.kowalski@example.com",
                Height = 175,
                Weight = 75m,
                ShoeSize = 43,
                SkillLevel = SkillLevel.Intermediate,
                PreferredStyle = SkiStyle.Classic,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            // Add more users as needed
        };

        modelBuilder.Entity<UserProfile>().HasData(users);
    }
}
```

---

## Database Provider Configuration

### SQLite Configuration (Development/Mobile)

```csharp
public class SQLiteDbContextFactory : IDbContextFactory<SkiRentalDbContext>
{
    public SkiRentalDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SkiRentalDbContext>();
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "SkiRental.db");
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        return new SkiRentalDbContext(optionsBuilder.Options);
    }
}
```

### SQL Server Configuration (Production)

```csharp
public class ProductionDbContextFactory : IDbContextFactory<SkiRentalDbContext>
{
    private readonly string _connectionString;

    public ProductionDbContextFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public SkiRentalDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SkiRentalDbContext>();
        
        optionsBuilder.UseSqlServer(_connectionString, options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
        
        return new SkiRentalDbContext(optionsBuilder.Options);
    }
}
```

### In-Memory Configuration (Testing)

```csharp
public class InMemoryDbContextFactory : IDbContextFactory<SkiRentalDbContext>
{
    private readonly string _databaseName;

    public InMemoryDbContextFactory(string databaseName = "TestDb")
    {
        _databaseName = databaseName;
    }

    public SkiRentalDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SkiRentalDbContext>();
        
        optionsBuilder.UseInMemoryDatabase(_databaseName);
        
        return new SkiRentalDbContext(optionsBuilder.Options);
    }
}
```

---

## Query Optimization

### Efficient Queries

#### Equipment Availability Query

```csharp
public async Task<IReadOnlyList<Equipment>> GetAvailableEquipmentAsync(
    EquipmentCategory category, 
    SkiStyle style, 
    CancellationToken ct = default)
{
    return await _context.Equipment
        .Where(e => e.Category == category && 
                   e.Status == EquipmentStatus.Available &&
                   (e.CompatibleStyle == style || e.CompatibleStyle == null))
        .OrderBy(e => Math.Abs(e.Size - GetOptimalSize(style, 175)))
        .AsNoTracking()
        .ToListAsync(ct);
}
```

#### Rental Analytics Query

```csharp
public async Task<RentalAnalytics> GetRentalAnalyticsAsync(
    DateTime startDate, 
    DateTime endDate,
    CancellationToken ct = default)
{
    return await _context.Rentals
        .Where(r => r.StartTime >= startDate && r.StartTime <= endDate)
        .GroupBy(r => new { r.Status, r.StartTime.Date })
        .Select(g => new RentalAnalytics
        {
            Date = g.Key.Date,
            Status = g.Key.Status,
            Count = g.Count(),
            TotalRevenue = g.Sum(r => r.TotalAmount)
        })
        .AsNoTracking()
        .ToListAsync(ct);
}
```

### Index Strategy

#### Performance-Critical Indexes

```sql
-- Equipment availability queries
CREATE INDEX IX_Equipment_Availability 
ON Equipment(Status, Category, CompatibleStyle) 
WHERE Status = 0; -- Available

-- Rental date range queries
CREATE INDEX IX_Rentals_DateRange 
ON Rentals(StartTime, EndTime) 
WHERE Status IN (1, 2); -- Active or Completed

-- Payment status queries
CREATE INDEX IX_Payments_StatusDate 
ON Payments(Status, CreatedAt);
```

---

## Database Backup and Recovery

### SQLite Backup Strategy

```csharp
public class SQLiteBackupService
{
    private readonly string _databasePath;
    private readonly string _backupDirectory;

    public SQLiteBackupService(IConfiguration configuration)
    {
        _databasePath = configuration.GetConnectionString("DefaultConnection")
            .Replace("Data Source=", "");
        _backupDirectory = configuration["Backup:Directory"] ?? "./backups";
    }

    public async Task CreateBackupAsync()
    {
        var backupFileName = $"SkiRental_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
        var backupPath = Path.Combine(_backupDirectory, backupFileName);

        Directory.CreateDirectory(_backupDirectory);

        using var sourceConnection = new SqliteConnection($"Data Source={_databasePath}");
        using var backupConnection = new SqliteConnection($"Data Source={backupPath}");
        
        await sourceConnection.OpenAsync();
        await backupConnection.OpenAsync();
        
        sourceConnection.BackupDatabase(backupConnection);
    }

    public async Task RestoreBackupAsync(string backupFileName)
    {
        var backupPath = Path.Combine(_backupDirectory, backupFileName);
        
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file not found");

        var tempPath = Path.GetTempFileName();
        
        try
        {
            // Copy current database to temp
            File.Copy(_databasePath, tempPath, true);
            
            // Replace current with backup
            File.Copy(backupPath, _databasePath, true);
        }
        catch
        {
            // Restore from temp if backup failed
            if (File.Exists(tempPath))
                File.Copy(tempPath, _databasePath, true);
            
            throw;
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
```

---

## Database Monitoring

### Performance Monitoring

```csharp
public class DatabaseMonitor
{
    private readonly SkiRentalDbContext _context;
    private readonly ILogger<DatabaseMonitor> _logger;

    public async Task<DatabaseMetrics> GetMetricsAsync()
    {
        var metrics = new DatabaseMetrics
        {
            TotalUsers = await _context.UserProfiles.CountAsync(),
            TotalEquipment = await _context.Equipment.CountAsync(),
            ActiveRentals = await _context.Rentals.CountAsync(r => r.Status == RentalStatus.Active),
            AvailableEquipment = await _context.Equipment.CountAsync(e => e.Status == EquipmentStatus.Available),
            DatabaseSize = await GetDatabaseSizeAsync(),
            LastBackupTime = await GetLastBackupTimeAsync()
        };

        return metrics;
    }

    private async Task<long> GetDatabaseSizeAsync()
    {
        // SQLite specific query to get database size
        await using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT page_count * page_size FROM pragma_page_count(), pragma_page_size()";
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task<DateTime?> GetLastBackupTimeAsync()
    {
        // Implementation depends on backup tracking strategy
        return null;
    }
}
```

---

## Data Validation and Integrity

### Custom Validation Rules

```csharp
public class DatabaseValidationService
{
    private readonly SkiRentalDbContext _context;

    public async Task<ValidationResult> ValidateDataIntegrityAsync()
    {
        var result = new ValidationResult();
        
        // Validate rental equipment consistency
        await ValidateRentalEquipmentIntegrity(result);
        
        // Validate payment consistency
        await ValidatePaymentIntegrity(result);
        
        // Validate equipment status consistency
        await ValidateEquipmentStatusIntegrity(result);
        
        return result;
    }

    private async Task ValidateRentalEquipmentIntegrity(ValidationResult result)
    {
        // Check for rentals without equipment
        var rentalsWithoutEquipment = await _context.Rentals
            .Where(r => !r.RentalEquipments.Any())
            .ToListAsync();
        
        foreach (var rental in rentalsWithoutEquipment)
        {
            result.AddError($"Rental {rental.Id} has no associated equipment");
        }
        
        // Check for equipment assigned to multiple active rentals
        var equipmentConflicts = await _context.RentalEquipments
            .Join(_context.Equipment, re => re.EquipmentId, e => e.Id, (re, e) => new { re, e })
            .Join(_context.Rentals, combined => combined.re.RentalId, r => r.Id, (combined, r) => new { combined.re, combined.e, r })
            .Where(x => x.r.Status == RentalStatus.Active)
            .GroupBy(x => x.e.Id)
            .Where(g => g.Count() > 1)
            .ToListAsync();
        
        foreach (var conflict in equipmentConflicts)
        {
            result.AddError($"Equipment {conflict.Key} is assigned to {conflict.Count()} active rentals");
        }
    }

    private async Task ValidatePaymentIntegrity(ValidationResult result)
    {
        // Check for rentals with payments that don't match total amount
        var paymentMismatches = await _context.Rentals
            .Join(_context.Payments, r => r.Id, p => p.RentalId, (r, p) => new { r, p })
            .Where(x => x.r.TotalAmount != x.p.Amount)
            .ToListAsync();
        
        foreach (var mismatch in paymentMismatches)
        {
            result.AddWarning($"Payment amount {mismatch.p.Amount} doesn't match rental total {mismatch.r.TotalAmount}");
        }
    }

    private async Task ValidateEquipmentStatusIntegrity(ValidationResult result)
    {
        // Check for equipment marked as available but assigned to active rental
        var statusConflicts = await _context.RentalEquipments
            .Join(_context.Equipment, re => re.EquipmentId, e => e.Id, (re, e) => new { re, e })
            .Join(_context.Rentals, combined => combined.re.RentalId, r => r.Id, (combined, r) => new { combined.re, combined.e, r })
            .Where(x => x.r.Status == RentalStatus.Active && x.e.Status == EquipmentStatus.Available)
            .ToListAsync();
        
        foreach (var conflict in statusConflicts)
        {
            result.AddError($"Equipment {conflict.e.Id} is marked as available but assigned to active rental {conflict.r.Id}");
        }
    }
}
```

---

## Database Security

### Connection Security

```csharp
public class SecureDbContextFactory : IDbContextFactory<SkiRentalDbContext>
{
    private readonly IConfiguration _configuration;

    public SecureDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SkiRentalDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SkiRentalDbContext>();
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        // Configure secure connection
        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.EnableRetryOnFailure();
            options.CommandTimeout(30);
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
        
        // Enable sensitive data logging only in development
        if (_configuration["Environment"] == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
        
        return new SkiRentalDbContext(optionsBuilder.Options);
    }
}
```

### Data Encryption

```csharp
public class DataEncryptionService
{
    private readonly string _encryptionKey;

    public DataEncryptionService(IConfiguration configuration)
    {
        _encryptionKey = configuration["Encryption:Key"] ?? 
            throw new InvalidOperationException("Encryption key not configured");
    }

    public string EncryptSensitiveData(string data)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        
        // Write IV to the beginning of the stream
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(data);
        }
        
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string DecryptSensitiveData(string encryptedData)
    {
        var fullCipher = Convert.FromBase64String(encryptedData);
        
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        
        // Extract IV from the beginning of the cipher
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];
        
        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }
}
```

---

## Troubleshooting

### Common Database Issues

#### Migration Failures

**Symptoms**: Migration commands fail with errors

**Causes**:
1. Database schema conflicts
2. Missing dependencies
3. Insufficient permissions

**Solutions**:
```bash
# Reset database (development only)
dotnet ef database drop --force
dotnet ef database update

# Create new migration
dotnet ef migrations add FixIssue
dotnet ef database update
```

#### Performance Issues

**Symptoms**: Slow query performance

**Causes**:
1. Missing indexes
2. Inefficient queries
3. Large dataset

**Solutions**:
```csharp
// Add missing indexes
await _context.Database.ExecuteSqlRawAsync(
    "CREATE INDEX IF NOT EXISTS IX_Equipment_Performance ON Equipment(Status, Category)");

// Optimize queries with AsNoTracking
var equipment = await _context.Equipment
    .AsNoTracking()
    .Where(e => e.Status == EquipmentStatus.Available)
    .ToListAsync();
```

#### Connection Issues

**Symptoms**: Unable to connect to database

**Causes**:
1. Incorrect connection string
2. Database file permissions
3. Network connectivity

**Solutions**:
```csharp
// Test connection
try
{
    await _context.Database.CanConnectAsync();
    Console.WriteLine("Database connection successful");
}
catch (Exception ex)
{
    Console.WriteLine($"Database connection failed: {ex.Message}");
}
```

---

## Future Enhancements

### Planned Database Features

#### Advanced Analytics Schema

```sql
-- Equipment usage tracking
CREATE TABLE EquipmentUsageLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EquipmentId INTEGER NOT NULL,
    RentalId INTEGER NOT NULL,
    UsageStartTime TEXT NOT NULL,
    UsageEndTime TEXT,
    UsageType INTEGER NOT NULL, -- 0=Rental, 1=Maintenance, 2=Inspection
    Notes TEXT,
    FOREIGN KEY (EquipmentId) REFERENCES Equipment(Id),
    FOREIGN KEY (RentalId) REFERENCES Rentals(Id)
);

-- Customer preferences and behavior
CREATE TABLE CustomerPreferences (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL,
    PreferenceType INTEGER NOT NULL, -- 0=Equipment, 1=Route, 2=Time
    PreferenceValue TEXT NOT NULL,
    Weight REAL NOT NULL DEFAULT 1.0,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (CustomerId) REFERENCES UserProfiles(Id)
);
```

#### Audit Trail Schema

```sql
CREATE TABLE AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TableName TEXT NOT NULL,
    RecordId INTEGER NOT NULL,
    Action TEXT NOT NULL, -- INSERT, UPDATE, DELETE
    OldValues TEXT, -- JSON
    NewValues TEXT, -- JSON
    ChangedBy INTEGER,
    ChangedAt TEXT NOT NULL,
    FOREIGN KEY (ChangedBy) REFERENCES UserProfiles(Id)
);
```

#### Performance Monitoring Schema

```sql
CREATE TABLE PerformanceMetrics (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MetricType TEXT NOT NULL, -- QueryTime, ConnectionTime, etc.
    MetricValue REAL NOT NULL,
    MetricUnit TEXT,
    RecordedAt TEXT NOT NULL,
    AdditionalData TEXT -- JSON for extra context
);
```

---

This comprehensive database schema and Entity Framework Core documentation provides complete coverage of the data persistence layer in the SkiRental application. The schema is designed for performance, scalability, and data integrity while maintaining flexibility for future enhancements.
