# SkiRental.Tests - Testing Project Documentation

## Overview

The `SkiRental.Tests` project contains comprehensive unit tests for the SkiRental application's business logic and domain models. It uses xUnit as the testing framework, Moq for mocking dependencies, and FluentAssertions for readable test assertions.

## Project Structure

```
SkiRental.Tests/
├── SkiRental.Tests.csproj           # Project configuration and dependencies
├── EquipmentMathServiceTests.cs    # Equipment recommendation algorithm tests
├── AuthenticationServiceTests.cs     # Authentication system tests
├── BookingServiceTests.cs         # Rental booking workflow tests
├── PaymentServiceTests.cs          # Payment processing tests
├── AnalyticsServiceTests.cs        # Business analytics tests
├── RepositoryTests/
│   ├── EfEquipmentRepositoryTests.cs    # Equipment data access tests
│   └── EfRentalRepositoryTests.cs       # Rental data access tests
├── TestData/
│   ├── TestDataBuilder.cs              # Test data factory
│   ├── TestScenarios.cs                # Common test scenarios
│   └── MockFactories.cs                # Mock object factories
└── Utilities/
    ├── TestConfiguration.cs            # Test setup configuration
    └── AssertionHelpers.cs             # Custom assertion helpers
```

## Testing Framework Configuration

### Dependencies

**File**: `SkiRental.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SkiRental.Domain\SkiRental.Domain.csproj" />
    <ProjectReference Include="..\SkiRental.Logic\SkiRental.Logic.csproj" />
    <ProjectReference Include="..\SkiRental.App\SkiRental.App.csproj" />
  </ItemGroup>

</Project>
```

### Test Configuration

**File**: `Utilities/TestConfiguration.cs`

```csharp
public static class TestConfiguration
{
    public static IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
            builder.AddProvider(new TestLoggerProvider()));

        // In-memory database
        services.AddDbContext<SkiRentalDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Test repositories
        services.AddScoped<IEquipmentRepository, TestEquipmentRepository>();
        services.AddScoped<IRentalRepository, TestRentalRepository>();

        // Business services
        services.AddScoped<EquipmentMathService>();
        services.AddScoped<BookingService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<AnalyticsService>();

        return services;
    }

    public static DbContextOptions<SkiRentalDbContext> CreateInMemoryDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder<SkiRentalDbContext>();
        builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        return builder.Options;
    }
}
```

---

## Core Test Classes

### EquipmentMathServiceTests

**File**: `EquipmentMathServiceTests.cs`

Comprehensive tests for the intelligent equipment recommendation algorithm.

#### Test Coverage

| Test Category | Test Method | Scenario |
|---------------|-------------|----------|
| **Ski Length Calculation** | `CalculateSkiLength_Classic_NormalBmi` | Classic style, normal BMI |
| | `CalculateSkiLength_Classic_Beginner` | Classic style, beginner level |
| | `CalculateSkiLength_Classic_Expert` | Classic style, expert level |
| | `CalculateSkiLength_Skate_NormalBmi` | Skate style, normal BMI |
| | `CalculateSkiLength_Overweight` | Overweight user correction |
| | `CalculateSkiLength_Underweight` | Underweight user correction |
| **Pole Length Calculation** | `CalculatePoleLength_Classic` | Classic style poles |
| | `CalculatePoleLength_Skate` | Skate style poles |
| | `CalculatePoleLength_Rounding` | Proper rounding behavior |
| **Stiffness Calculation** | `CalculateStiffness_BySkillLevel` | Skill level mapping |
| | `CalculateStiffness_BmiCorrection` | BMI adjustments |
| | `CalculateStiffness_AlwaysWithinBounds` | Boundary validation |
| **Integration Tests** | `RecommendAsync_ReturnsRecommendations` | Full recommendation flow |
| | `RecommendAsync_WithEquipmentAvailability` | Real equipment matching |
| | `RecommendAsync_BmiFlagSetCorrectly` | BMI correction flag |

#### Example Test Implementation

```csharp
public class EquipmentMathServiceTests
{
    private readonly Mock<IEquipmentRepository> _mockRepo;
    private readonly Mock<ILogger<EquipmentMathService>> _mockLogger;
    private readonly EquipmentMathService _service;

    public EquipmentMathServiceTests()
    {
        _mockRepo = new Mock<IEquipmentRepository>();
        _mockLogger = new Mock<ILogger<EquipmentMathService>>();
        _service = new EquipmentMathService(_mockRepo.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData(175, 75, SkiStyle.Classic, SkillLevel.Beginner, 195.0)] // 175 + 20
    [InlineData(175, 75, SkiStyle.Classic, SkillLevel.Intermediate, 200.0)] // 175 + 25
    [InlineData(175, 75, SkiStyle.Classic, SkillLevel.Expert, 205.0)] // 175 + 30
    [InlineData(175, 75, SkiStyle.Skate, SkillLevel.Beginner, 185.0)] // 175 + 10
    [InlineData(175, 75, SkiStyle.Skate, SkillLevel.Intermediate, 187.5)] // 175 + 12.5
    [InlineData(175, 75, SkiStyle.Skate, SkillLevel.Expert, 190.0)] // 175 + 15
    public void CalculateSkiLength_ReturnsCorrectValues(
        int height, decimal weight, SkiStyle style, SkillLevel level, decimal expected)
    {
        // Arrange
        var parameters = new EquipmentSelectionParameters
        {
            Height = height,
            Weight = weight,
            Style = style,
            SkillLevel = level,
            ShoeSize = 43
        };

        // Act
        var result = _service.CalculateSkiLength(parameters);

        // Assert
        result.Should().BeApproximately(expected, 0.1m);
    }

    [Theory]
    [InlineData(175, 75, SkiStyle.Classic, 148.75)] // 175 × 0.85
    [InlineData(175, 75, SkiStyle.Skate, 157.5)] // 175 × 0.90
    public void CalculatePoleLengthCm_ReturnsCorrectValues(
        int height, decimal weight, SkiStyle style, decimal expected)
    {
        // Arrange
        var parameters = new EquipmentSelectionParameters
        {
            Height = height,
            Weight = weight,
            Style = style,
            SkillLevel = SkillLevel.Intermediate,
            ShoeSize = 43
        };

        // Act
        var result = _service.CalculatePoleLengthCm(parameters);

        // Assert
        result.Should().BeApproximately(expected, 0.1m);
    }

    [Theory]
    [InlineData(SkillLevel.Beginner, 42)]
    [InlineData(SkillLevel.Intermediate, 52)]
    [InlineData(SkillLevel.Advanced, 62)]
    [InlineData(SkillLevel.Expert, 72)]
    public void CalculateStiffness_ReturnsBaseValues(SkillLevel level, int expected)
    {
        // Arrange
        var parameters = CreateNormalBmiParameters();
        parameters.SkillLevel = level;

        // Act
        var result = _service.CalculateStiffness(parameters);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateStiffness_OverweightUser_AddsCorrection()
    {
        // Arrange
        var parameters = new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 95m, // BMI = 31.0 (overweight)
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Intermediate,
            ShoeSize = 43
        };

        // Act
        var result = _service.CalculateStiffness(parameters);

        // Assert
        result.Should().Be(57); // 52 (base) + 5 (BMI correction)
    }

    [Fact]
    public void CalculateStiffness_UnderweightUser_SubtractsCorrection()
    {
        // Arrange
        var parameters = new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 55m, // BMI = 18.0 (underweight)
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Intermediate,
            ShoeSize = 43
        };

        // Act
        var result = _service.CalculateStiffness(parameters);

        // Assert
        result.Should().Be(47); // 52 (base) - 5 (BMI correction)
    }

    [Fact]
    public void CalculateStiffness_AlwaysWithinBounds_ClampsValues()
    {
        // Arrange - Test extreme values
        var overweightParameters = new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 150m, // Very high BMI
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Expert, // 72 + 5 = 77 (should clamp to 90)
            ShoeSize = 43
        };

        var underweightParameters = new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 40m, // Very low BMI
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Beginner, // 42 - 5 = 37 (should clamp to 30)
            ShoeSize = 43
        };

        // Act
        var overweightResult = _service.CalculateStiffness(overweightParameters);
        var underweightResult = _service.CalculateStiffness(underweightParameters);

        // Assert
        overweightResult.Should().BeLessThanOrEqualTo(90);
        underweightResult.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public async Task RecommendAsync_ReturnsExplanation()
    {
        // Arrange
        var parameters = CreateNormalBmiParameters();
        SetupMockEquipment();

        // Act
        var result = await _service.RecommendAsync(parameters);

        // Assert
        result.AlgorithmExplanation.Should().NotBeNullOrEmpty();
        result.AlgorithmExplanation.Should().Contain("175");
        result.AlgorithmExplanation.Should().Contain(parameters.Style.ToString());
    }

    [Fact]
    public async Task RecommendAsync_WithOverweight_BmiFlagSetCorrectly()
    {
        // Arrange
        var parameters = new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 90m, // BMI = 29.4 (overweight)
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Intermediate,
            ShoeSize = 43
        };
        SetupMockEquipment();

        // Act
        var result = await _service.RecommendAsync(parameters);

        // Assert
        result.BmiCorrectionApplied.Should().BeTrue();
        result.BmiCorrectionNote.Should().Contain("nadwaga");
        result.BmiCorrectionNote.Should().Contain("5 cm");
    }

    [Fact]
    public async Task RecommendAsync_FiltersEquipmentByAvailability()
    {
        // Arrange
        var parameters = CreateNormalBmiParameters();
        var availableEquipment = CreateTestEquipment();
        var rentedEquipment = CreateTestEquipment();
        rentedEquipment.Status = EquipmentStatus.Rented;

        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            parameters.Style, EquipmentCategory.Skis, It.IsAny<CancellationToken>()))
            .ReturnsAsync([availableEquipment]);

        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            parameters.Style, EquipmentCategory.Poles, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Equipment>());

        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            parameters.Style, EquipmentCategory.Boots, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Equipment>());

        // Act
        var result = await _service.RecommendAsync(parameters);

        // Assert
        result.MatchingEquipment.Should().Contain(e => e.Id == availableEquipment.Id);
        result.MatchingEquipment.Should().NotContain(e => e.Status == EquipmentStatus.Rented);
    }

    [Fact]
    public async Task RecommendAsync_MatchesEquipmentBySizeTolerance()
    {
        // Arrange
        var parameters = new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 75m,
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Intermediate,
            ShoeSize = 43
        };

        var perfectMatch = CreateTestEquipment(size: 200m); // Perfect match
        var closeMatch = CreateTestEquipment(size: 195m);   // Within tolerance
        var outOfRange = CreateTestEquipment(size: 180m);  // Outside tolerance

        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            parameters.Style, EquipmentCategory.Skis, It.IsAny<CancellationToken>()))
            .ReturnsAsync([perfectMatch, closeMatch, outOfRange]);

        // Act
        var result = await _service.RecommendAsync(parameters);

        // Assert
        result.MatchingEquipment.Should().Contain(e => e.Id == perfectMatch.Id);
        result.MatchingEquipment.Should().Contain(e => e.Id == closeMatch.Id);
        result.MatchingEquipment.Should().NotContain(e => e.Id == outOfRange.Id);
    }

    // Helper Methods
    private static EquipmentSelectionParameters CreateNormalBmiParameters()
    {
        return new EquipmentSelectionParameters
        {
            Height = 175,
            Weight = 75m, // BMI = 24.5 (normal)
            Style = SkiStyle.Classic,
            SkillLevel = SkillLevel.Intermediate,
            ShoeSize = 43
        };
    }

    private static Equipment CreateTestEquipment(decimal size = 200m)
    {
        return new Equipment
        {
            Id = 1,
            SerialNumber = "TEST-001",
            Brand = "TestBrand",
            Model = "TestModel",
            Category = EquipmentCategory.Skis,
            Size = size,
            Status = EquipmentStatus.Available,
            DailyRate = 50m
        };
    }

    private void SetupMockEquipment()
    {
        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            It.IsAny<SkiStyle>(), EquipmentCategory.Skis, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateTestEquipment()]);

        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            It.IsAny<SkiStyle>(), EquipmentCategory.Poles, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateTestPoles()]);

        _mockRepo.Setup(r => r.GetAvailableByStyleAsync(
            It.IsAny<SkiStyle>(), EquipmentCategory.Boots, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateTestBoots()]);
    }

    private static Equipment CreateTestPoles()
    {
        return new Equipment
        {
            Id = 2,
            SerialNumber = "POLE-001",
            Brand = "TestBrand",
            Model = "TestPole",
            Category = EquipmentCategory.Poles,
            Size = 150m,
            Status = EquipmentStatus.Available,
            DailyRate = 15m
        };
    }

    private static Equipment CreateTestBoots()
    {
        return new Equipment
        {
            Id = 3,
            SerialNumber = "BOOT-001",
            Brand = "TestBrand",
            Model = "TestBoot",
            Category = EquipmentCategory.Boots,
            Size = 43m,
            Status = EquipmentStatus.Available,
            DailyRate = 25m
        };
    }
}
```

### AuthenticationServiceTests

**File**: `AuthenticationServiceTests.cs`

Tests for user authentication and authorization functionality.

#### Test Categories

| Test Method | Scenario | Expected Result |
|-------------|----------|-----------------|
| `LoginAsync_ValidCredentials_ReturnsSuccess` | Correct username/password | Success, authenticated |
| `LoginAsync_InvalidPassword_ReturnsFailure` | Wrong password | Failure, not authenticated |
| `LoginAsync_InvalidUsername_ReturnsFailure` | Non-existent username | Failure, not authenticated |
| `LoginAsync_EmptyCredentials_ReturnsFailure` | Empty inputs | Validation error |
| `LoginAsync_WorkerRole_SetsCorrectRole` | Worker login | Worker role assigned |
| `LoginAsync_CustomerRole_SetsCorrectRole` | Customer login | Customer role assigned |
| `Logout_ClearsAuthenticationState` | Logout after login | All state cleared |
| `AuthenticationChanged_EventFired` | Login/logout events | Event triggered |

#### Example Implementation

```csharp
public class AuthenticationServiceTests
{
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _authService = new AuthenticationService();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var username = "admin";
        var password = "password";

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Logowanie pomyślne");
        
        _authService.IsAuthenticated.Should().BeTrue();
        _authService.CurrentUsername.Should().Be("Administrator");
        _authService.CurrentUserId.Should().Be("1");
        _authService.CurrentRole.Should().Be(UserRole.Worker);
    }

    [Theory]
    [InlineData("admin", "wrongpassword")]
    [InlineData("nonexistent", "password")]
    [InlineData("", "password")]
    [InlineData("admin", "")]
    public async Task LoginAsync_InvalidCredentials_ReturnsFailure(string username, string password)
    {
        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("nieprawidłowa");
        
        _authService.IsAuthenticated.Should().BeFalse();
        _authService.CurrentUsername.Should().BeNull();
        _authService.CurrentUserId.Should().BeNull();
        _authService.CurrentRole.Should().Be(UserRole.Customer);
    }

    [Fact]
    public async Task LoginAsync_WorkerRole_SetsCorrectRole()
    {
        // Arrange
        var username = "worker";
        var password = "worker123";

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeTrue();
        _authService.CurrentRole.Should().Be(UserRole.Worker);
    }

    [Fact]
    public async Task LoginAsync_CustomerRole_SetsCorrectRole()
    {
        // Arrange
        var username = "user";
        var password = "user123";

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeTrue();
        _authService.CurrentRole.Should().Be(UserRole.Customer);
    }

    [Fact]
    public void Logout_ClearsAuthenticationState()
    {
        // Arrange
        _authService.LoginAsync("admin", "password").Wait();
        _authService.IsAuthenticated.Should().BeTrue();

        // Act
        _authService.Logout();

        // Assert
        _authService.IsAuthenticated.Should().BeFalse();
        _authService.CurrentUsername.Should().BeNull();
        _authService.CurrentUserId.Should().BeNull();
        _authService.CurrentRole.Should().Be(UserRole.Customer);
    }

    [Fact]
    public void AuthenticationChanged_EventFired()
    {
        // Arrange
        var eventFired = false;
        _authService.AuthenticationChanged += () => eventFired = true;

        // Act
        _authService.Logout();

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var username = "newuser";
        var email = "newuser@example.com";
        var password = "password123";
        var confirmPassword = "password123";

        // Act
        var result = await _authService.RegisterAsync(username, email, password, confirmPassword);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("pomyślna");
    }

    [Theory]
    [InlineData("", "test@example.com", "password123", "password123")]
    [InlineData("test", "", "password123", "password123")]
    [InlineData("test", "test@example.com", "", "password123")]
    [InlineData("test", "test@example.com", "short", "short")]
    [InlineData("test", "test@example.com", "password123", "different")]
    public async Task RegisterAsync_InvalidData_ReturnsFailure(string username, string email, string password, string confirmPassword)
    {
        // Act
        var result = await _authService.RegisterAsync(username, email, password, confirmPassword);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }
}
```

---

## Test Data Management

### TestDataBuilder

**File**: `TestData/TestDataBuilder.cs`

Factory class for creating consistent test data across all tests.

```csharp
public static class TestDataBuilder
{
    // Equipment Creation
    public static Equipment CreateTestEquipment(
        EquipmentCategory category = EquipmentCategory.Skis,
        decimal size = 180m,
        EquipmentStatus status = EquipmentStatus.Available,
        SkiStyle? compatibleStyle = null)
    {
        return new Equipment
        {
            Id = Random.Shared.Next(1, 10000),
            SerialNumber = $"TEST-{Guid.NewGuid():N}",
            Brand = "TestBrand",
            Model = "TestModel",
            Category = category,
            Size = size,
            Status = status,
            CompatibleStyle = compatibleStyle,
            YearOfProduction = DateTime.Now.Year,
            DailyRate = GetDefaultRate(category),
            Stiffness = category == EquipmentCategory.Skis ? 50 : null
        };
    }

    public static Equipment CreateTestSkis(decimal size = 180m, SkiStyle style = SkiStyle.Classic)
    {
        return CreateTestEquipment(EquipmentCategory.Skis, size, EquipmentStatus.Available, style);
    }

    public static Equipment CreateTestPoles(decimal length = 150m)
    {
        return CreateTestEquipment(EquipmentCategory.Poles, length);
    }

    public static Equipment CreateTestBoots(int size = 43)
    {
        return CreateTestEquipment(EquipmentCategory.Boots, size);
    }

    // User Profile Creation
    public static UserProfile CreateTestUserProfile(
        int height = 175,
        decimal weight = 75m,
        int shoeSize = 43,
        SkillLevel skillLevel = SkillLevel.Intermediate,
        SkiStyle preferredStyle = SkiStyle.Classic)
    {
        return new UserProfile
        {
            Id = Random.Shared.Next(1, 10000),
            FirstName = "Test",
            LastName = "User",
            Email = $"test{Random.Shared.Next(1, 10000)}@example.com",
            Height = height,
            Weight = weight,
            ShoeSize = shoeSize,
            SkillLevel = skillLevel,
            PreferredStyle = preferredStyle,
            PhoneNumber = $"+48{Random.Shared.Next(100000000, 999999999)}",
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365))
        };
    }

    public static UserProfile CreateOverweightUser()
    {
        return CreateTestUserProfile(height: 175, weight: 95m); // BMI ~31
    }

    public static UserProfile CreateUnderweightUser()
    {
        return CreateTestUserProfile(height: 175, weight: 55m); // BMI ~18
    }

    // Rental Creation
    public static Rental CreateTestRental(
        int customerId = 1,
        DateTime? startTime = null,
        DateTime? endTime = null,
        RentalStatus status = RentalStatus.Active)
    {
        var now = DateTime.UtcNow;
        return new Rental
        {
            Id = Random.Shared.Next(1, 10000),
            CustomerProfileId = customerId,
            StartTime = startTime ?? now.AddHours(-2),
            EndTime = endTime ?? now.AddHours(2),
            Status = status,
            TotalAmount = 100m,
            QrCode = $"QR-{Guid.NewGuid():N}",
            CreatedAt = startTime ?? now.AddHours(-2)
        };
    }

    public static Rental CreateCompletedRental(int customerId = 1)
    {
        var now = DateTime.UtcNow;
        return CreateTestRental(
            customerId,
            now.AddHours(-6),
            now.AddHours(-2),
            RentalStatus.Completed
        );
    }

    public static Rental CreateOverdueRental(int customerId = 1)
    {
        var now = DateTime.UtcNow;
        return CreateTestRental(
            customerId,
            now.AddHours(-6),
            now.AddHours(-2),
            RentalStatus.Active
        );
    }

    // Equipment Selection Parameters
    public static EquipmentSelectionParameters CreateSelectionParameters(
        int height = 175,
        decimal weight = 75m,
        int shoeSize = 43,
        SkillLevel skillLevel = SkillLevel.Intermediate,
        SkiStyle style = SkiStyle.Classic)
    {
        return new EquipmentSelectionParameters
        {
            Height = height,
            Weight = weight,
            ShoeSize = shoeSize,
            SkillLevel = skillLevel,
            Style = style
        };
    }

    // Helper Methods
    private static decimal GetDefaultRate(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Skis => 50m,
            EquipmentCategory.Poles => 15m,
            EquipmentCategory.Boots => 25m,
            EquipmentCategory.Gloves => 10m,
            EquipmentCategory.Helmet => 20m,
            _ => 30m
        };
    }

    // Collections
    public static List<Equipment> CreateTestEquipmentCollection(int count = 10)
    {
        var categories = Enum.GetValues<EquipmentCategory>();
        var equipment = new List<Equipment>();

        for (int i = 0; i < count; i++)
        {
            var category = categories[Random.Shared.Next(categories.Length)];
            var size = GetRandomSize(category);
            equipment.Add(CreateTestEquipment(category, size));
        }

        return equipment;
    }

    public static List<UserProfile> CreateTestUserCollection(int count = 5)
    {
        var users = new List<UserProfile>();
        var skillLevels = Enum.GetValues<SkillLevel>();
        var styles = Enum.GetValues<SkiStyle>();

        for (int i = 0; i < count; i++)
        {
            var height = Random.Shared.Next(150, 200);
            var weight = (decimal)(height * 0.4 + Random.Shared.Next(-10, 20));
            var shoeSize = Random.Shared.Next(38, 48);
            var skillLevel = skillLevels[Random.Shared.Next(skillLevels.Length)];
            var style = styles[Random.Shared.Next(styles.Length)];

            users.Add(CreateTestUserProfile(height, weight, shoeSize, skillLevel, style));
        }

        return users;
    }

    private static decimal GetRandomSize(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Skis => (decimal)Random.Shared.Next(160, 210),
            EquipmentCategory.Poles => (decimal)Random.Shared.Next(120, 170),
            EquipmentCategory.Boots => Random.Shared.Next(35, 50),
            EquipmentCategory.Gloves => (decimal)Random.Shared.Next(7, 12),
            EquipmentCategory.Helmet => (decimal)Random.Shared.Next(52, 62),
            _ => 100m
        };
    }
}
```

### TestScenarios

**File**: `TestData/TestScenarios.cs`

Predefined test scenarios for common use cases.

```csharp
public static class TestScenarios
{
    // User Scenarios
    public static class Users
    {
        public static UserProfile BeginnerUser => TestDataBuilder.CreateTestUserProfile(
            height: 170, weight: 65m, shoeSize: 40, 
            skillLevel: SkillLevel.Beginner, preferredStyle: SkiStyle.Classic);

        public static UserProfile ExpertSkateUser => TestDataBuilder.CreateTestUserProfile(
            height: 185, weight: 85m, shoeSize: 45, 
            skillLevel: SkillLevel.Expert, preferredStyle: SkiStyle.Skate);

        public static UserProfile OverweightIntermediateUser => TestDataBuilder.CreateOverweightUser()
            with { SkillLevel = SkillLevel.Intermediate, PreferredStyle = SkiStyle.Classic };

        public static UserProfile UnderweightAdvancedUser => TestDataBuilder.CreateUnderweightUser()
            with { SkillLevel = SkillLevel.Advanced, PreferredStyle = SkiStyle.Skate };
    }

    // Equipment Scenarios
    public static class Equipment
    {
        public static Equipment ClassicBeginnerSkis => TestDataBuilder.CreateTestSkis(185m, SkiStyle.Classic)
            with { Stiffness = 42 };

        public static Equipment SkateExpertSkis => TestDataBuilder.CreateTestSkis(195m, SkiStyle.Skate)
            with { Stiffness = 72 };

        public static List<Equipment> MixedFleet => new()
        {
            TestDataBuilder.CreateTestSkis(180m, SkiStyle.Classic),
            TestDataBuilder.CreateTestSkis(185m, SkiStyle.Skate),
            TestDataBuilder.CreateTestPoles(150m),
            TestDataBuilder.CreateTestPoles(157m),
            TestDataBuilder.CreateTestBoots(42),
            TestDataBuilder.CreateTestBoots(44)
        };

        public static List<Equipment> RentedFleet => MixedFleet.Select(e => 
            TestDataBuilder.CreateTestEquipment(e.Category, e.Size, EquipmentStatus.Rented)).ToList();
    }

    // Rental Scenarios
    public static class Rentals
    {
        public static Rental ActiveRental => TestDataBuilder.CreateTestRental(status: RentalStatus.Active);

        public static Rental CompletedRental => TestDataBuilder.CreateCompletedRental();

        public static Rental OverdueRental => TestDataBuilder.CreateOverdueRental();

        public static List<Rental> TypicalDay => new()
        {
            TestDataBuilder.CreateTestRental(status: RentalStatus.Completed),
            TestDataBuilder.CreateTestRental(status: RentalStatus.Active),
            TestDataBuilder.CreateOverdueRental(),
            TestDataBuilder.CreateCompletedRental()
        };
    }

    // Business Scenarios
    public static class Business
    {
        public static class PeakSeason
        {
            public static List<Rental> HighVolume => Enumerable.Range(0, 20)
                .Select(i => TestDataBuilder.CreateTestRental(
                    customerId: Random.Shared.Next(1, 10),
                    startTime: DateTime.Today.AddHours(Random.Shared.Next(8, 18)),
                    endTime: DateTime.Today.AddHours(Random.Shared.Next(19, 22))
                )).ToList();

            public static List<Equipment> HighUtilization => TestDataBuilder.CreateTestEquipmentCollection(15)
                .Select(e => TestDataBuilder.CreateTestEquipment(
                    e.Category, e.Size, EquipmentStatus.Rented)).ToList();
        }

        public static class LowSeason
        {
            public static List<Rental> LowVolume => new()
            {
                TestDataBuilder.CreateTestRental(status: RentalStatus.Completed),
                TestDataBuilder.CreateTestRental(status: RentalStatus.Active)
            };

            public static List<Equipment> LowUtilization => TestDataBuilder.CreateTestEquipmentCollection(10)
                .Select(e => TestDataBuilder.CreateTestEquipment(
                    e.Category, e.Size, EquipmentStatus.Available)).ToList();
        }
    }

    // Edge Cases
    public static class EdgeCases
    {
        public static UserProfile ExtremeHeightUser => TestDataBuilder.CreateTestUserProfile(
            height: 250, weight: 120m, shoeSize: 50);

        public static UserProfile MinimalHeightUser => TestDataBuilder.CreateTestUserProfile(
            height: 100, weight: 30m, shoeSize: 30);

        public static Equipment ExtremeSizeSkis => TestDataBuilder.CreateTestSkis(250m);

        public static Equipment MinimalSizeSkis => TestDataBuilder.CreateTestSkis(100m);

        public static Rental VeryLongRental => TestDataBuilder.CreateTestRental(
            startTime: DateTime.UtcNow.AddDays(-1),
            endTime: DateTime.UtcNow.AddDays(1),
            status: RentalStatus.Active);

        public static Rental VeryShortRental => TestDataBuilder.CreateTestRental(
            startTime: DateTime.UtcNow.AddMinutes(-30),
            endTime: DateTime.UtcNow.AddMinutes(30),
            status: RentalStatus.Active);
    }
}
```

---

## Mock Factories

### MockFactories

**File**: `TestData/MockFactories.cs`

Factory methods for creating mock objects with realistic behavior.

```csharp
public static class MockFactories
{
    // Repository Mocks
    public static Mock<IEquipmentRepository> CreateEquipmentRepositoryMock(
        List<Equipment>? availableEquipment = null,
        List<Equipment>? allEquipment = null)
    {
        var mock = new Mock<IEquipmentRepository>();

        availableEquipment ??= TestDataBuilder.CreateTestEquipmentCollection();
        allEquipment ??= availableEquipment;

        mock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) =>
                allEquipment.FirstOrDefault(e => e.Id == id));

        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEquipment.AsReadOnly());

        mock.Setup(r => r.GetAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availableEquipment.Where(e => e.Status == EquipmentStatus.Available).ToList());

        mock.Setup(r => r.GetAvailableByStyleAsync(
            It.IsAny<SkiStyle>(), It.IsAny<EquipmentCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkiStyle style, EquipmentCategory category, CancellationToken ct) =>
                availableEquipment.Where(e => e.Category == category && 
                                         e.Status == EquipmentStatus.Available &&
                                         (e.CompatibleStyle == style || e.CompatibleStyle == null)).ToList());

        mock.Setup(r => r.CreateAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipment equipment, CancellationToken ct) =>
            {
                equipment.Id = Random.Shared.Next(1, 10000);
                availableEquipment.Add(equipment);
                allEquipment.Add(equipment);
                return equipment;
            });

        mock.Setup(r => r.UpdateAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipment equipment, CancellationToken ct) => equipment);

        mock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((int id, CancellationToken ct) =>
            {
                var equipment = availableEquipment.FirstOrDefault(e => e.Id == id);
                if (equipment != null)
                {
                    availableEquipment.Remove(equipment);
                }
            })
            .Returns(Task.CompletedTask);

        return mock;
    }

    public static Mock<IRentalRepository> CreateRentalRepositoryMock(
        List<Rental>? rentals = null)
    {
        var mock = new Mock<IRentalRepository>();
        rentals ??= new List<Rental>();

        mock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) =>
                rentals.FirstOrDefault(r => r.Id == id));

        mock.Setup(r => r.GetByCustomerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int customerId, CancellationToken ct) =>
                rentals.Where(r => r.CustomerProfileId == customerId).ToList());

        mock.Setup(r => r.GetActiveRentalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rentals.Where(r => r.Status == RentalStatus.Active).ToList());

        mock.Setup(r => r.GetOverdueRentalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rentals.Where(r => r.Status == RentalStatus.Active && 
                                         r.EndTime < DateTime.UtcNow).ToList());

        mock.Setup(r => r.CreateAsync(It.IsAny<Rental>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rental rental, CancellationToken ct) =>
            {
                rental.Id = Random.Shared.Next(1, 10000);
                rentals.Add(rental);
                return rental;
            });

        mock.Setup(r => r.UpdateAsync(It.IsAny<Rental>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rental rental, CancellationToken ct) => rental);

        mock.Setup(r => r.GetRentalsByDateRangeAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime start, DateTime end, CancellationToken ct) =>
                rentals.Where(r => r.StartTime >= start && r.StartTime <= end).ToList());

        return mock;
    }

    // Service Mocks
    public static Mock<IPaymentProvider> CreatePaymentProviderMock(
        bool shouldSucceed = true)
    {
        var mock = new Mock<IPaymentProvider>();

        mock.Setup(p => p.InitiateBlikPayment(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = shouldSucceed,
                TransactionId = shouldSucceed ? Guid.NewGuid().ToString() : null,
                Message = shouldSucceed ? "Payment successful" : "Payment failed"
            });

        mock.Setup(p => p.PreauthorizePayment(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = shouldSucceed,
                TransactionId = shouldSucceed ? Guid.NewGuid().ToString() : null,
                Message = shouldSucceed ? "Pre-authorization successful" : "Pre-authorization failed"
            });

        mock.Setup(p => p.CapturePreauthorization(
            It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = shouldSucceed,
                Message = shouldSucceed ? "Capture successful" : "Capture failed"
            });

        mock.Setup(p => p.RefundPayment(
            It.IsAny<string>(), It.IsAny<decimal?>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = shouldSucceed,
                Message = shouldSucceed ? "Refund successful" : "Refund failed"
            });

        mock.Setup(p => p.CheckPaymentStatus(It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = shouldSucceed,
                Message = shouldSucceed ? "Payment completed" : "Payment pending"
            });

        return mock;
    }

    // Logger Mocks
    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        var mock = new Mock<ILogger<T>>();
        
        mock.Setup(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Func<object, Exception?, string>>(
                (level, id, state, exception, formatter) =>
                {
                    // Log to console for debugging during tests
                    Console.WriteLine($"[{level}] {formatter(state, exception)}");
                });

        return mock;
    }

    // HttpClient Mocks
    public static Mock<HttpMessageHandler> CreateHttpMessageHandlerMock(
        HttpResponseMessage response)
    {
        var mock = new Mock<HttpMessageHandler>();
        
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return mock;
    }

    public static HttpClient CreateHttpClientMock(string responseContent)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        var mockHandler = CreateHttpMessageHandlerMock(response);
        return new HttpClient(mockHandler.Object);
    }

    // DbContext Mocks
    public static SkiRentalDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SkiRentalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SkiRentalDbContext(options);
    }

    public static SkiRentalDbContext CreateDbContextWithData()
    {
        var context = CreateInMemoryDbContext();
        
        // Add test data
        context.Equipments.AddRange(TestDataBuilder.CreateTestEquipmentCollection());
        context.UserProfiles.AddRange(TestDataBuilder.CreateTestUserCollection());
        context.SaveChanges();

        return context;
    }
}
```

---

## Custom Assertion Helpers

### AssertionHelpers

**File**: `Utilities/AssertionHelpers.cs`

Custom assertion methods for more readable test assertions.

```csharp
public static class AssertionHelpers
{
    // Equipment Assertions
    public static void ShouldBeAvailableEquipment(this Equipment equipment)
    {
        equipment.Should().NotBeNull();
        equipment.Status.Should().Be(EquipmentStatus.Available);
    }

    public static void ShouldBeRentedEquipment(this Equipment equipment)
    {
        equipment.Should().NotBeNull();
        equipment.Status.Should().Be(EquipmentStatus.Rented);
    }

    public static void ShouldMatchParameters(this Equipment equipment, EquipmentSelectionParameters parameters, decimal tolerance = 5m)
    {
        equipment.Category.Should().BeOneOf(
            parameters.Style == SkiStyle.Classic 
                ? new[] { EquipmentCategory.Skis, EquipmentCategory.Poles, EquipmentCategory.Boots }
                : new[] { EquipmentCategory.Skis, EquipmentCategory.Poles, EquipmentCategory.Boots });

        if (equipment.Category == EquipmentCategory.Skis)
        {
            var expectedLength = parameters.Style == SkiStyle.Classic 
                ? parameters.Height + 25m 
                : parameters.Height + 12.5m;
            equipment.Size.Should().BeApproximately(expectedLength, tolerance);
        }
        else if (equipment.Category == EquipmentCategory.Poles)
        {
            var expectedLength = parameters.Style == SkiStyle.Classic 
                ? parameters.Height * 0.85m 
                : parameters.Height * 0.90m;
            equipment.Size.Should().BeApproximately(expectedLength, tolerance);
        }
        else if (equipment.Category == EquipmentCategory.Boots)
        {
            equipment.Size.Should().Be(parameters.ShoeSize);
        }
    }

    // User Profile Assertions
    public static void ShouldHaveNormalBmi(this UserProfile user)
    {
        user.Bmi.Should().BeGreaterThanOrEqualTo(18.5m);
        user.Bmi.Should().BeLessThanOrEqualTo(24.9m);
        user.IsBmiNormal.Should().BeTrue();
        user.IsOverweight.Should().BeFalse();
        user.IsUnderweight.Should().BeFalse();
    }

    public static void ShouldBeOverweight(this UserProfile user)
    {
        user.Bmi.Should().BeGreaterThan(24.9m);
        user.IsBmiNormal.Should().BeFalse();
        user.IsOverweight.Should().BeTrue();
        user.IsUnderweight.Should().BeFalse();
    }

    public static void ShouldBeUnderweight(this UserProfile user)
    {
        user.Bmi.Should().BeLessThan(18.5m);
        user.IsBmiNormal.Should().BeFalse();
        user.IsOverweight.Should().BeFalse();
        user.IsUnderweight.Should().BeTrue();
    }

    // Rental Assertions
    public static void ShouldBeActiveRental(this Rental rental)
    {
        rental.Should().NotBeNull();
        rental.Status.Should().Be(RentalStatus.Active);
        rental.StartTime.Should().BeBeforeOrEqualTo(DateTime.UtcNow);
        rental.EndTime.Should().BeAfter(DateTime.UtcNow);
    }

    public static void ShouldBeCompletedRental(this Rental rental)
    {
        rental.Should().NotBeNull();
        rental.Status.Should().Be(RentalStatus.Completed);
        rental.ActualReturnTime.Should().NotBeNull();
        rental.ActualReturnTime.Should().BeAfter(rental.StartTime);
    }

    public static void ShouldBeOverdueRental(this Rental rental)
    {
        rental.Should().NotBeNull();
        rental.Status.Should().Be(RentalStatus.Active);
        rental.EndTime.Should().BeBefore(DateTime.UtcNow);
    }

    // Payment Assertions
    public static void ShouldBeSuccessfulPayment(this PaymentResult result)
    {
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TransactionId.Should().NotBeNullOrEmpty();
        result.Message.Should().NotContain("failed", StringComparison.OrdinalIgnoreCase);
    }

    public static void ShouldBeFailedPayment(this PaymentResult result)
    {
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    // Recommendation Assertions
    public static void ShouldContainValidRecommendations(this EquipmentRecommendation recommendation)
    {
        recommendation.Should().NotBeNull();
        recommendation.RecommendedSkiLength.Should().BeGreaterThan(0);
        recommendation.RecommendedPoleLength.Should().BeGreaterThan(0);
        recommendation.RecommendedStiffness.Should().BeGreaterThan(0);
        recommendation.RecommendedBootSize.Should().BeGreaterThan(0);
        recommendation.AlgorithmExplanation.Should().NotBeNullOrEmpty();
    }

    public static void ShouldHaveBmiCorrection(this EquipmentRecommendation recommendation, bool shouldHaveCorrection)
    {
        recommendation.BmiCorrectionApplied.Should().Be(shouldHaveCorrection);
        
        if (shouldHaveCorrection)
        {
            recommendation.BmiCorrectionNote.Should().NotBeNullOrEmpty();
            recommendation.BmiCorrectionNote.Should().Contain("BMI", StringComparison.OrdinalIgnoreCase);
        }
    }

    // Collection Assertions
    public static void ShouldContainEquipmentForCategory(
        this IEnumerable<Equipment> equipment, 
        EquipmentCategory category, 
        int minimumCount = 1)
    {
        equipment.Should().NotBeNull();
        equipment.Count(e => e.Category == category).Should().BeGreaterOrEqualTo(minimumCount);
    }

    public static void ShouldAllBeAvailable(this IEnumerable<Equipment> equipment)
    {
        equipment.Should().NotBeNull();
        equipment.Should().OnlyContain(e => e.Status == EquipmentStatus.Available);
    }

    public static void ShouldHaveUtilizationRate(this UtilizationReport report, decimal expectedRate, decimal tolerance = 0.05m)
    {
        report.Should().NotBeNull();
        report.UtilizationRate.Should().BeApproximately(expectedRate, tolerance);
    }

    // Date and Time Assertions
    public static void ShouldBeWithinLastHours(this DateTime dateTime, int hours)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        dateTime.Should().BeOnOrAfter(cutoff);
        dateTime.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    public static void ShouldBeWithinNextHours(this DateTime dateTime, int hours)
    {
        var cutoff = DateTime.UtcNow.AddHours(hours);
        dateTime.Should().BeOnOrBefore(cutoff);
        dateTime.Should().BeOnOrAfter(DateTime.UtcNow);
    }

    // Numeric Assertions
    public static void ShouldBeApproximately(this decimal actual, decimal expected, decimal tolerance)
    {
        var difference = Math.Abs(actual - expected);
        difference.Should().BeLessOrEqualTo(tolerance);
    }

    public static void ShouldBeValidSkiLength(this decimal length, int userHeight)
    {
        length.Should().BeGreaterThan(userHeight + 5); // Minimum reasonable offset
        length.Should().BeLessThan(userHeight + 50); // Maximum reasonable offset
    }

    public static void ShouldBeValidPoleLength(this decimal length, int userHeight)
    {
        length.Should().BeGreaterThan(userHeight * 0.7m); // Minimum reasonable ratio
        length.Should().BeLessThan(userHeight * 1.1m); // Maximum reasonable ratio
    }

    public static void ShouldBeValidStiffness(this int stiffness)
    {
        stiffness.Should().BeGreaterThanOrEqualTo(30);
        stiffness.Should().BeLessThanOrEqualTo(90);
    }
}
```

---

## Test Execution and Reporting

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~EquipmentMathServiceTests"

# Run tests by category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run tests in parallel
dotnet test --parallel
```

### Test Configuration

```json
// runsettings.json
{
  "$schema": "https://json.schemastore.org/v1-0-0/runsettings.schema.json",
  "logger": [
    {
      "logger": "console",
      "verbosity": "normal"
    },
    {
      "logger": "trx",
      "verbosity": "normal"
    }
  ],
  "testRunParameters": {
    "ParameterName": "ParameterValue",
    "ParameterName2": "ParameterValue2"
  },
  "EnvironmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Testing"
  }
}
```

### Continuous Integration

**GitHub Actions Example**:

```yaml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: ./TestResults/*/coverage.cobertura.xml
        flags: unittests
        name: codecov-umbrella
```

---

## Performance Testing

### Benchmark Tests

```csharp
[MemoryDiagnoser]
[SimpleJob]
public class EquipmentMathServiceBenchmarks
{
    private EquipmentMathService _service = null!;
    private EquipmentSelectionParameters _parameters = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mockRepo = new Mock<IEquipmentRepository>();
        var mockLogger = new Mock<ILogger<EquipmentMathService>>();
        _service = new EquipmentMathService(mockRepo.Object, mockLogger.Object);
        
        _parameters = TestDataBuilder.CreateSelectionParameters();
    }

    [Benchmark]
    public decimal CalculateSkiLength() => _service.CalculateSkiLength(_parameters);

    [Benchmark]
    public decimal CalculatePoleLength() => _service.CalculatePoleLengthCm(_parameters);

    [Benchmark]
    public int CalculateStiffness() => _service.CalculateStiffness(_parameters);

    [Benchmark]
    public async Task<EquipmentRecommendation> RecommendAsync() 
        => await _service.RecommendAsync(_parameters);
}
```

---

## Test Maintenance

### Test Data Management

1. **Version Control**: Test data should be versioned with the code
2. **Data Refresh**: Periodically refresh test data to reflect current business rules
3. **Privacy**: Ensure test data doesn't contain real customer information
4. **Consistency**: Use factories and builders for consistent test data

### Test Organization

1. **Naming Convention**: Use descriptive test names that explain the scenario
2. **Test Categories**: Group tests by functionality (Unit, Integration, Performance)
3. **Documentation**: Document complex test scenarios and business rules
4. **Cleanup**: Ensure tests clean up after themselves to avoid side effects

### Best Practices

1. **AAA Pattern**: Arrange, Act, Assert structure for all tests
2. **Single Responsibility**: Each test should verify one specific behavior
3. **Isolation**: Tests should not depend on each other
4. **Deterministic**: Tests should produce consistent results
5. **Fast Execution**: Unit tests should run quickly

---

## Coverage Goals

### Target Coverage

| Component | Target Coverage | Current Coverage |
|------------|-----------------|------------------|
| Domain Models | 95% | TBD |
| Business Logic | 90% | TBD |
| Services | 85% | TBD |
| Repositories | 80% | TBD |
| Overall | 85% | TBD |

### Coverage Exclusions

- Generated code (auto-generated classes)
- Test infrastructure code
- Exception classes (simple data holders)
- External API integration points (integration tests preferred)

---

## Future Testing Enhancements

### Planned Improvements

1. **Integration Tests**: Database and external service integration
2. **End-to-End Tests**: Full application workflow testing
3. **Performance Tests**: Load testing and performance benchmarks
4. **Accessibility Tests**: UI accessibility compliance
5. **Security Tests**: Penetration testing and vulnerability scanning

### Testing Tools

1. **Playwright**: End-to-end web testing
2. **Appium**: Mobile application testing
3. **Postman/Newman**: API testing
4. **OWASP ZAP**: Security testing
5. **Lighthouse**: Performance and accessibility testing

---

## Version History

- **v1.0**: Initial test suite with basic unit tests
- **v1.1**: Added comprehensive equipment math service tests
- **v1.2**: Added authentication and service tests
- **v1.3**: Added test data factories and mock helpers
- **v1.4**: Added performance benchmarks and CI integration
- **v1.5**: Enhanced coverage and assertion helpers
