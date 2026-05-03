# SkiRental.UI.Shared - Shared UI Components Documentation

## Overview

The `SkiRental.UI.Shared` project is a Razor Class Library (RCL) containing reusable Blazor components that can be shared across multiple Blazor applications. It provides consistent UI components for booking workflows, employee operations, and analytics dashboards.

## Project Structure

```
SkiRental.UI.Shared/
├── Components/
│   ├── Booking/
│   │   └── BookingWizard.razor       # Multi-step booking process
│   ├── Employee/
│   │   └── QrScannerPanel.razor      # QR code scanning interface
│   └── Analytics/
│       └── UtilizationDashboard.razor # Analytics dashboard
├── wwwroot/
│   ├── css/
│   │   ├── booking.css              # Booking component styles
│   │   ├── employee.css             # Employee panel styles
│   │   └── analytics.css            # Dashboard styles
│   └── js/
│       └── components.js            # Shared JavaScript functionality
└── SkiRental.UI.Shared.csproj
```

## Component Architecture

### Component Categories

1. **Booking Components**: Customer-facing rental workflows
2. **Employee Components**: Staff operational interfaces
3. **Analytics Components**: Business intelligence dashboards
4. **Shared Utilities**: Common UI patterns and helpers

---

## Booking Components

### BookingWizard

**File**: `Components/Booking/BookingWizard.razor`

A multi-step wizard component that guides customers through the equipment rental process with intelligent equipment recommendations.

#### Purpose

- Collect customer physical data and preferences
- Provide intelligent equipment recommendations
- Handle payment processing
- Generate QR codes for equipment return

#### Step Structure

| Step | Purpose | Key Features |
|------|---------|--------------|
| 1 - Profile Data | Collect user measurements | Height, weight, skill level, style preference |
| 2 - Equipment Selection | Show recommendations | AI-powered matching, availability display |
| 3 - Payment & Confirmation | Process payment | BLIK integration, QR code generation |

#### Component Interface

```razor
@* Usage Example *@
<BookingWizard 
    OnBookingCompleted="@HandleBookingCompleted"
    OnBookingCancelled="@HandleBookingCancelled"
    DefaultRentalDuration="@TimeSpan.FromHours(4)" />
```

#### Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `OnBookingCompleted` | `EventCallback<BookingResult>` | Fired on successful booking | Required |
| `OnBookingCancelled` | `EventCallback` | Fired when user cancels | Optional |
| `DefaultRentalDuration` | `TimeSpan` | Default rental period | 4 hours |
| `ShowAdvancedOptions` | `bool` | Display advanced equipment options | false |
| `EnableRecommendations` | `bool` | Use AI recommendations | true |

#### State Management

```csharp
public class BookingWizardState
{
    public int CurrentStep { get; set; } = 1;
    public UserProfileData ProfileData { get; set; } = new();
    public EquipmentSelection EquipmentSelection { get; set; } = new();
    public PaymentData PaymentData { get; set; } = new();
    public List<EquipmentRecommendation> Recommendations { get; set; } = new();
    public bool IsProcessing { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### Step 1: Profile Data Collection

**Features**:
- **Physical Measurements**: Height, weight, shoe size
- **Skill Assessment**: Skiing level selection with descriptions
- **Style Preference**: Classic vs Skate selection
- **Validation**: Real-time input validation with helpful messages

**UI Elements**:
```razor
<div class="profile-step">
    <EditForm Model="@state.ProfileData" OnValidSubmit="@ValidateProfile">
        <DataAnnotationsValidator />
        
        <div class="measurement-group">
            <label>Wzrost (cm)</label>
            <InputNumber @bind-Value="state.ProfileData.Height" />
            <ValidationMessage For="@(() => state.ProfileData.Height)" />
        </div>
        
        <div class="skill-selector">
            <h4>Poziom zaawansowania</h4>
            @foreach (var level in Enum.GetValues<SkillLevel>())
            {
                <div class="skill-option">
                    <InputRadioGroup @bind-Value="state.ProfileData.SkillLevel">
                        <InputRadio Value="@level" />
                        <span class="skill-label">@GetSkillDescription(level)</span>
                    </InputRadioGroup>
                </div>
            }
        </div>
    </EditForm>
</div>
```

#### Step 2: Equipment Recommendations

**Features**:
- **AI Recommendations**: Based on user profile data
- **Availability Display**: Real-time equipment availability
- **Visual Selection**: Equipment images and specifications
- **Alternative Options**: Manual selection override

**Recommendation Display**:
```razor
<div class="recommendations-grid">
    @foreach (var recommendation in state.Recommendations)
    {
        <div class="equipment-card @(recommendation.IsSelected ? "selected" : "")"
             @onclick="() => SelectEquipment(recommendation)">
            <img src="@recommendation.Equipment.ImagePath ?? '/images/default-equipment.png'" 
                 alt="@recommendation.Equipment.Model" />
            <div class="equipment-info">
                <h4>@recommendation.Equipment.Brand @recommendation.Equipment.Model</h4>
                <p class="recommendation-details">
                    @recommendation.AlgorithmExplanation
                </p>
                <div class="price">@recommendation.Equipment.DailyRate:C} / dzień</div>
            </div>
            <div class="selection-indicator">
                @if (recommendation.IsSelected) { <span>✓</span> }
            </div>
        </div>
    }
</div>
```

#### Step 3: Payment and Confirmation

**Features**:
- **Payment Integration**: BLIK mobile payment
- **Cost Breakdown**: Detailed pricing information
- **QR Code Generation**: Return verification code
- **Confirmation Receipt**: Digital receipt generation

**Payment Interface**:
```razor
<div class="payment-step">
    <div class="cost-summary">
        <h4>Podsumowanie kosztów</h4>
        <div class="cost-item">
            <span>Sprzęt (@state.EquipmentSelection.Count szt.)</span>
            <span>@CalculateEquipmentCost():C}</span>
        </div>
        <div class="cost-item">
            <span>Czas wynajmu (@state.ProfileData.RentalDuration h)</span>
            <span>@CalculateRentalCost():C}</span>
        </div>
        <div class="cost-total">
            <span>Łącznie</span>
            <span>@CalculateTotalCost():C</span>
        </div>
    </div>
    
    <div class="payment-methods">
        <h4>Metoda płatności</h4>
        <button class="payment-method blik" @onclick="ProcessBlikPayment">
            <span class="payment-icon">📱</span>
            <span>Zapłać BLIK</span>
        </button>
    </div>
</div>
```

#### Event Callbacks

```csharp
private async Task HandleBookingCompleted(BookingResult result)
{
    // Navigate to confirmation page
    NavigationManager.NavigateTo($"/booking-confirmation/{result.RentalId}");
    
    // Show success notification
    await ShowNotification("Wypożyczenie zostało pomyślnie zrealizowane!", "success");
}

private void HandleBookingCancelled()
{
    // Navigate back to home
    NavigationManager.NavigateTo("/");
}
```

---

## Employee Components

### QrScannerPanel

**File**: `Components/Employee/QrScannerPanel.razor`

A comprehensive employee interface for processing equipment returns using QR code scanning and NFC technology.

#### Purpose

- Scan QR codes for rental returns
- Evaluate equipment condition
- Process returns and payments
- Manage rental status updates

#### Component Interface

```razor
<QrScannerPanel 
    OnReturnProcessed="@HandleReturnProcessed"
    OnErrorOccurred="@HandleError"
    EnableNfcScanning="true"
    AutoFocusScanner="true" />
```

#### Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `OnReturnProcessed` | `EventCallback<ReturnResult>` | Fired on successful return processing | Required |
| `OnErrorOccurred` | `EventCallback<string>` | Fired on errors | Optional |
| `EnableNfcScanning` | `bool` | Enable NFC tag reading | true |
| `AutoFocusScanner` | `bool` | Auto-focus camera scanner | true |
| `ShowAdvancedOptions` | `bool` | Display advanced return options | false |

#### Scanner Interface

**QR Code Scanning**:
```razor
<div class="scanner-container">
    <div class="scanner-viewfinder">
        @if (isScanning)
        {
            <div class="scanning-animation">
                <div class="scan-line"></div>
            </div>
        }
        
        <div class="scanner-instructions">
            @if (string.IsNullOrEmpty(lastScannedCode))
            {
                <p>Skieruj kamerę na kod QR klienta</p>
            }
            else
            {
                <p class="success">Kod wykryty: @lastScannedCode</p>
            }
        </div>
    </div>
    
    <div class="scanner-controls">
        <button class="btn btn-primary" @onclick="StartScanning" disabled="@isScanning">
            Rozpocznij skanowanie
        </button>
        <button class="btn btn-secondary" @onclick="StopScanning" disabled="!isScanning">
            Zatrzymaj skanowanie
        </button>
    </div>
</div>
```

**JavaScript Integration**:
```javascript
// wwwroot/js/components.js
window.skiRentalBridge = {
    dispatchQrCode: function(qrCode) {
        // Dispatch QR code to Blazor component
        DotNet.invokeMethodAsync('QrScannerPanel', 'OnQrCodeDetected', qrCode);
    },
    
    startQrScanner: function(dotNetReference) {
        // Initialize QR scanner with Blazor reference
        this.dotNetRef = dotNetReference;
        // Scanner initialization logic
    },
    
    stopQrScanner: function() {
        // Stop scanner and cleanup
    }
};
```

#### Return Processing Workflow

**Step 1: QR Code Detection**
```csharp
[JSInvokable("OnQrCodeDetected")]
public async Task OnQrCodeDetected(string qrCode)
{
    lastScannedCode = qrCode;
    isScanning = false;
    
    await ProcessRentalReturn(qrCode);
    StateHasChanged();
}
```

**Step 2: Rental Lookup**
```csharp
private async Task ProcessRentalReturn(string qrCode)
{
    try
    {
        isProcessing = true;
        errorMessage = null;
        
        // Find rental by QR code
        var rental = await rentalService.GetByQrCodeAsync(qrCode);
        if (rental == null)
        {
            errorMessage = "Nie znaleziono wypożyczenia dla podanego kodu QR";
            return;
        }
        
        currentRental = rental;
        await LoadReturnDetails(rental);
    }
    catch (Exception ex)
    {
        errorMessage = $"Błąd podczas przetwarzania zwrotu: {ex.Message}";
        await OnErrorOccurred.InvokeAsync(errorMessage);
    }
    finally
    {
        isProcessing = false;
    }
}
```

**Step 3: Condition Evaluation**
```razor
<div class="condition-evaluation">
    <h4>Ocena stanu sprzętu</h4>
    
    @foreach (var equipment in currentRental.RentalEquipments)
    {
        <div class="equipment-condition">
            <div class="equipment-info">
                <h5>@equipment.Equipment.Brand @equipment.Equipment.Model</h5>
                <span>Numer seryjny: @equipment.Equipment.SerialNumber</span>
            </div>
            
            <div class="condition-rating">
                <label>Stan przy zwrocie (1-5):</label>
                <div class="rating-stars">
                    @for (int i = 1; i <= 5; i++)
                    {
                        <button type="button" 
                                class="star @(equipment.ConditionAtReturn == i ? "selected" : "")"
                                @onclick="() => SetConditionRating(equipment, i)">
                            @i
                        </button>
                    }
                </div>
                <small class="rating-hint">
                    1 - Bardzo zły, 5 - Doskonały
                </small>
            </div>
        </div>
    }
</div>
```

**Step 4: Final Processing**
```csharp
private async Task CompleteReturnAsync()
{
    try
    {
        isProcessing = true;
        
        // Update equipment conditions
        foreach (var equipment in currentRental.RentalEquipments)
        {
            await equipmentService.UpdateConditionAsync(
                equipment.EquipmentId, 
                equipment.ConditionAtReturn ?? 5);
        }
        
        // Process final payment
        var paymentResult = await paymentService.CapturePaymentAsync(
            currentRental.PayUOrderId, 
            CalculateFinalAmount());
        
        // Update rental status
        await rentalService.CompleteReturnAsync(currentRental.Id, DateTime.Now);
        
        // Generate return receipt
        var receipt = await GenerateReturnReceipt(currentRental);
        
        await OnReturnProcessed.InvokeAsync(new ReturnResult
        {
            RentalId = currentRental.Id,
            Receipt = receipt,
            FinalAmount = CalculateFinalAmount()
        });
        
        // Reset for next return
        ResetForm();
    }
    catch (Exception ex)
    {
        errorMessage = $"Błąd podczas finalizacji zwrotu: {ex.Message}";
    }
    finally
    {
        isProcessing = false;
    }
}
```

#### NFC Integration

**NFC Reading**:
```csharp
private async Task StartNfcScanningAsync()
{
    if (!await nfcService.IsAvailableAsync())
    {
        errorMessage = "NFC nie jest dostępne na tym urządzeniu";
        return;
    }
    
    try
    {
        isNfcScanning = true;
        var nfcData = await nfcService.ReadTagAsync();
        
        if (!string.IsNullOrEmpty(nfcData))
        {
            await ProcessRentalReturn(nfcData);
        }
    }
    catch (Exception ex)
    {
        errorMessage = $"Błąd odczytu NFC: {ex.Message}";
    }
    finally
    {
        isNfcScanning = false;
    }
}
```

---

## Analytics Components

### UtilizationDashboard

**File**: `Components/Analytics/UtilizationDashboard.razor`

A comprehensive analytics dashboard for monitoring equipment utilization, revenue, and operational metrics.

#### Purpose

- Display equipment utilization rates
- Show revenue analytics
- Monitor maintenance schedules
- Provide operational insights

#### Component Interface

```razor
<UtilizationDashboard 
    DateRange="@selectedDateRange"
    OnDateRangeChanged="@HandleDateRangeChanged"
    RefreshInterval="30000"
    ShowDetailedMetrics="true" />
```

#### Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `DateRange` | `DateRange` | Analytics date range | Last 30 days |
| `OnDateRangeChanged` | `EventCallback<DateRange>` | Fired when date range changes | Optional |
| `RefreshInterval` | `int` | Auto-refresh interval in milliseconds | 30000 |
| `ShowDetailedMetrics` | `bool` | Show detailed breakdowns | true |
| `EnableExport` | `bool` | Enable data export | false |

#### Dashboard Layout

**Header with Controls**:
```razor
<div class="dashboard-header">
    <div class="date-range-selector">
        <select @bind="selectedDateRange.Preset" @onchange="UpdateDateRange">
            <option value="today">Dziś</option>
            <option value="week">Ostatni tydzień</option>
            <option value="month">Ostatni miesiąc</option>
            <option value="quarter">Ostatni kwartał</option>
            <option value="custom">Niestandardowy</option>
        </select>
        
        @if (selectedDateRange.Preset == "custom")
        {
            <input type="date" @bind="selectedDateRange.StartDate" />
            <span>do</span>
            <input type="date" @bind="selectedDateRange.EndDate" />
        }
        
        <button class="btn btn-primary" @onclick="RefreshData">
            Odśwież
        </button>
    </div>
    
    @if (EnableExport)
    {
        <div class="export-controls">
            <button class="btn btn-secondary" @onclick="ExportToExcel">
                📊 Eksportuj Excel
            </button>
            <button class="btn btn-secondary" @onclick="ExportToPdf">
                📄 Eksportuj PDF
            </button>
        </div>
    }
</div>
```

**Key Metrics Cards**:
```razor
<div class="metrics-grid">
    <div class="metric-card primary">
        <div class="metric-icon">🎿</div>
        <div class="metric-content">
            <h3>Wykorzystanie floty</h3>
            <div class="metric-value">@analyticsData.OverallUtilizationRate:P1%</div>
            <div class="metric-change @(analyticsData.UtilizationTrend > 0 ? "positive" : "negative")">
                @(analyticsData.UtilizationTrend > 0 ? "↑" : "↓") @Math.Abs(analyticsData.UtilizationTrend):P1%
            </div>
        </div>
    </div>
    
    <div class="metric-card success">
        <div class="metric-icon">💰</div>
        <div class="metric-content">
            <h3>Przychody</h3>
            <div class="metric-value">@analyticsData.TotalRevenue:C</div>
            <div class="metric-change @(analyticsData.RevenueTrend > 0 ? "positive" : "negative")">
                @(analyticsData.RevenueTrend > 0 ? "↑" : "↓") @Math.Abs(analyticsData.RevenueTrend):P1%
            </div>
        </div>
    </div>
    
    <div class="metric-card warning">
        <div class="metric-icon">🔧</div>
        <div class="metric-content">
            <h3>Sprzęt w serwisie</h3>
            <div class="metric-value">@analyticsData.EquipmentInMaintenance</div>
            <div class="metric-change">
                z @analyticsData.TotalEquipment sztuk
            </div>
        </div>
    </div>
    
    <div class="metric-card info">
        <div class="metric-icon">📈</div>
        <div class="metric-content">
            <h3>Aktywne wypożyczenia</h3>
            <div class="metric-value">@analyticsData.ActiveRentals</div>
            <div class="metric-change">
                @analyticsData.AverageRentalDuration:F1} h średnio
            </div>
        </div>
    </div>
</div>
```

**Utilization Charts**:
```razor
<div class="charts-section">
    <div class="chart-container">
        <h3>Wykorzystanie sprzętu - kategorie</h3>
        <div class="category-utilization-chart">
            @foreach (var category in analyticsData.CategoryUtilization)
            {
                <div class="category-bar">
                    <div class="category-label">@category.Category</div>
                    <div class="bar-container">
                        <div class="bar-fill" 
                             style="width: @(category.UtilizationRate * 100)%"></div>
                        <span class="bar-value">@category.UtilizationRate:P1%</span>
                    </div>
                </div>
            }
        </div>
    </div>
    
    <div class="chart-container">
        <h3>Przychody w czasie</h3>
        <div class="revenue-timeline">
            @foreach (var day in analyticsData.DailyRevenue)
            {
                <div class="day-bar" 
                     style="height: @(day.Revenue / analyticsData.MaxDailyRevenue * 100)%;"
                     title="@day.Date.ToString("dd.MM"): @day.Revenue:C">
                </div>
            }
        </div>
    </div>
</div>
```

**Equipment Performance Table**:
```razor
<div class="equipment-performance">
    <h3>Wydajność sprzętu</h3>
    <div class="performance-table">
        <table>
            <thead>
                <tr>
                    <th>Sprzęt</th>
                    <th>Kategoria</th>
                    <th>Wykorzystanie</th>
                    <th>Przychody</th>
                    <th>Ostatni serwis</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var equipment in analyticsData.EquipmentPerformance.Take(10))
                {
                    <tr class="@GetPerformanceRowClass(equipment.UtilizationRate)">
                        <td>@equipment.Brand @equipment.Model</td>
                        <td>@equipment.Category</td>
                        <td>
                            <div class="utilization-bar">
                                <div class="bar-fill" 
                                     style="width: @(equipment.UtilizationRate * 100)%"></div>
                                <span>@equipment.UtilizationRate:P1%</span>
                            </div>
                        </td>
                        <td>@equipment.TotalRevenue:C</td>
                        <td>
                            @(equipment.LastMaintenanceDate?.ToString("dd.MM.yy") ?? "Brak")
                        </td>
                        <td>
                            <span class="status-badge @equipment.Status.ToString().ToLower()">
                                @GetStatusDisplay(equipment.Status)
                            </span>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
```

#### Data Loading and Refresh

```csharp
protected override async Task OnInitializedAsync()
{
    await LoadAnalyticsData();
    
    // Set up auto-refresh
    timer = new Timer(async _ => 
    {
        await InvokeAsync(LoadAnalyticsData);
        StateHasChanged();
    }, null, RefreshInterval, RefreshInterval);
}

private async Task LoadAnalyticsData()
{
    try
    {
        isLoading = true;
        
        analyticsData = await analyticsService.GetComprehensiveAnalyticsAsync(
            selectedDateRange.StartDate, 
            selectedDateRange.EndDate);
    }
    catch (Exception ex)
    {
        errorMessage = $"Błąd ładowania danych analitycznych: {ex.Message}";
    }
    finally
    {
        isLoading = false;
    }
}
```

#### Export Functionality

```csharp
private async Task ExportToExcel()
{
    try
    {
        var excelData = await analyticsService.ExportToExcelAsync(
            selectedDateRange.StartDate, 
            selectedDateRange.EndDate);
        
        // Download file
        await jsRuntime.InvokeVoidAsync("downloadFile", 
            "analytics.xlsx", 
            Convert.ToBase64String(excelData));
    }
    catch (Exception ex)
    {
        errorMessage = $"Błąd eksportu do Excel: {ex.Message}";
    }
}
```

---

## Styling and Theming

### CSS Architecture

**File Structure**:
```
wwwroot/css/
├── booking.css          # Booking component styles
├── employee.css         # Employee panel styles  
├── analytics.css        # Dashboard styles
└── shared.css           # Common component styles
```

### Booking Component Styles

**Wizard Navigation**:
```css
.booking-wizard {
    max-width: 800px;
    margin: 0 auto;
    background: white;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.wizard-header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 2rem;
    border-radius: 12px 12px 0 0;
}

.step-indicators {
    display: flex;
    justify-content: space-between;
    margin-bottom: 2rem;
}

.step-indicator {
    flex: 1;
    text-align: center;
    position: relative;
}

.step-indicator::after {
    content: '';
    position: absolute;
    top: 15px;
    right: -50%;
    width: 100%;
    height: 2px;
    background: rgba(255, 255, 255, 0.3);
    z-index: 1;
}

.step-indicator:last-child::after {
    display: none;
}

.step-indicator.active .step-number {
    background: white;
    color: #667eea;
}

.step-indicator.completed .step-number {
    background: #4CAF50;
}

.step-number {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.3);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    position: relative;
    z-index: 2;
}
```

**Equipment Cards**:
```css
.equipment-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1.5rem;
    margin: 2rem 0;
}

.equipment-card {
    border: 2px solid #e0e0e0;
    border-radius: 12px;
    padding: 1rem;
    cursor: pointer;
    transition: all 0.3s ease;
    position: relative;
}

.equipment-card:hover {
    border-color: #667eea;
    transform: translateY(-2px);
    box-shadow: 0 8px 25px rgba(102, 126, 234, 0.15);
}

.equipment-card.selected {
    border-color: #4CAF50;
    background: #f8fff8;
}

.equipment-card img {
    width: 100%;
    height: 150px;
    object-fit: cover;
    border-radius: 8px;
    margin-bottom: 1rem;
}

.selection-indicator {
    position: absolute;
    top: 1rem;
    right: 1rem;
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: #4CAF50;
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
}
```

### Employee Panel Styles

**Scanner Interface**:
```css
.qr-scanner-panel {
    background: #1a1a1a;
    color: white;
    border-radius: 12px;
    padding: 2rem;
    min-height: 500px;
}

.scanner-viewfinder {
    position: relative;
    width: 300px;
    height: 300px;
    margin: 0 auto 2rem;
    border: 3px solid #4CAF50;
    border-radius: 12px;
    overflow: hidden;
}

.scanning-animation {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
}

.scan-line {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 3px;
    background: linear-gradient(90deg, transparent, #4CAF50, transparent);
    animation: scan 2s linear infinite;
}

@keyframes scan {
    0% { top: 0; }
    100% { top: 100%; }
}

.scanner-instructions {
    position: absolute;
    bottom: 1rem;
    left: 0;
    right: 0;
    text-align: center;
    background: rgba(0, 0, 0, 0.7);
    padding: 0.5rem;
}
```

**Condition Rating**:
```css
.condition-evaluation {
    background: white;
    color: #333;
    border-radius: 8px;
    padding: 1.5rem;
    margin: 1rem 0;
}

.equipment-condition {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 0;
    border-bottom: 1px solid #e0e0e0;
}

.rating-stars {
    display: flex;
    gap: 0.5rem;
}

.rating-stars button {
    width: 40px;
    height: 40px;
    border: 2px solid #e0e0e0;
    border-radius: 50%;
    background: white;
    cursor: pointer;
    transition: all 0.2s ease;
    font-weight: bold;
}

.rating-stars button:hover {
    border-color: #667eea;
    transform: scale(1.1);
}

.rating-stars button.selected {
    background: #667eea;
    color: white;
    border-color: #667eea;
}
```

### Analytics Dashboard Styles

**Metrics Grid**:
```css
.dashboard-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 2rem;
    padding: 1rem;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
}

.metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2rem;
}

.metric-card {
    background: white;
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
    display: flex;
    align-items: center;
    gap: 1rem;
    transition: transform 0.2s ease;
}

.metric-card:hover {
    transform: translateY(-2px);
}

.metric-card.primary { border-left: 4px solid #667eea; }
.metric-card.success { border-left: 4px solid #4CAF50; }
.metric-card.warning { border-left: 4px solid #FF9800; }
.metric-card.info { border-left: 4px solid #2196F3; }

.metric-icon {
    font-size: 2rem;
    opacity: 0.8;
}

.metric-value {
    font-size: 2rem;
    font-weight: bold;
    color: #333;
    margin: 0.5rem 0;
}

.metric-change {
    font-size: 0.9rem;
    font-weight: 500;
}

.metric-change.positive { color: #4CAF50; }
.metric-change.negative { color: #F44336; }
```

**Charts Section**:
```css
.charts-section {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 2rem;
    margin-bottom: 2rem;
}

.chart-container {
    background: white;
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
}

.category-utilization-chart {
    margin-top: 1rem;
}

.category-bar {
    display: flex;
    align-items: center;
    margin-bottom: 1rem;
}

.category-label {
    width: 120px;
    font-weight: 500;
}

.bar-container {
    flex: 1;
    height: 24px;
    background: #f0f0f0;
    border-radius: 12px;
    position: relative;
    overflow: hidden;
}

.bar-fill {
    height: 100%;
    background: linear-gradient(90deg, #667eea, #764ba2);
    border-radius: 12px;
    transition: width 0.5s ease;
}

.bar-value {
    position: absolute;
    right: 0.5rem;
    top: 50%;
    transform: translateY(-50%);
    font-weight: bold;
    color: white;
    font-size: 0.8rem;
}
```

---

## JavaScript Integration

### Component Bridge

**File**: `wwwroot/js/components.js`

```javascript
window.skiRentalBridge = {
    // QR Scanner Integration
    dispatchQrCode: function(qrCode) {
        if (this.qrScannerDotNetRef) {
            this.qrScannerDotNetRef.invokeMethodAsync('OnQrCodeDetected', qrCode);
        }
    },
    
    startQrScanner: function(dotNetReference) {
        this.qrScannerDotNetRef = dotNetReference;
        // Initialize camera and QR scanning
        this.initializeCamera();
    },
    
    stopQrScanner: function() {
        // Stop camera and cleanup
        this.stopCamera();
        this.qrScannerDotNetRef = null;
    },
    
    // File Download
    downloadFile: function(filename, base64Data) {
        const link = document.createElement('a');
        link.href = 'data:application/octet-stream;base64,' + base64Data;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },
    
    // Chart Initialization
    initializeChart: function(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (canvas) {
            // Initialize chart library (Chart.js, etc.)
            this.createChart(canvas, chartData);
        }
    },
    
    // Notification System
    showNotification: function(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 5000);
    }
};
```

### Camera Integration

```javascript
// QR Scanner Camera Management
initializeCamera: async function() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
        });
        
        const videoElement = document.getElementById('qr-video');
        if (videoElement) {
            videoElement.srcObject = stream;
            videoElement.play();
        }
        
        this.startQrDetection();
    } catch (error) {
        console.error('Camera initialization failed:', error);
        this.showNotification('Nie można uzyskać dostępu do kamery', 'error');
    }
},

startQrDetection: function() {
    // Initialize QR code detection library
    // (e.g., qr-scanner or jsQR)
    this.detectQrCodes();
},

detectQrCodes: function() {
    const videoElement = document.getElementById('qr-video');
    const canvas = document.getElementById('qr-canvas');
    const context = canvas.getContext('2d');
    
    const detect = () => {
        if (videoElement.readyState === videoElement.HAVE_ENOUGH_DATA) {
            canvas.width = videoElement.videoWidth;
            canvas.height = videoElement.videoHeight;
            context.drawImage(videoElement, 0, 0, canvas.width, canvas.height);
            
            const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
            const code = jsQR(imageData.data, imageData.width, imageData.height);
            
            if (code) {
                this.dispatchQrCode(code.data);
                this.stopCamera();
                return;
            }
        }
        
        requestAnimationFrame(detect);
    };
    
    detect();
}
```

---

## Component Testing

### Unit Testing Strategy

**Booking Wizard Tests**:
```csharp
public class BookingWizardTests
{
    [Fact]
    public void BookingWizard_ShouldValidateProfileData()
    {
        // Arrange
        var component = new BookingWizard();
        var invalidProfile = new UserProfileData 
        { 
            Height = 50, // Invalid height
            Weight = 200m,
            ShoeSize = 60 // Invalid shoe size
        };
        
        // Act
        var isValid = component.ValidateProfile(invalidProfile);
        
        // Assert
        Assert.False(isValid);
        Assert.Contains(component.ValidationErrors, 
            e => e.PropertyName == nameof(UserProfileData.Height));
    }
    
    [Fact]
    public async Task BookingWizard_ShouldGenerateRecommendations()
    {
        // Arrange
        var component = new BookingWizard();
        var validProfile = CreateValidProfile();
        
        // Act
        await component.LoadRecommendationsAsync(validProfile);
        
        // Assert
        Assert.NotEmpty(component.State.Recommendations);
        Assert.All(component.State.Recommendations, 
            r => Assert.NotNull(r.Equipment));
    }
}
```

### Integration Testing

**QR Scanner Integration**:
```csharp
public class QrScannerPanelTests
{
    [Fact]
    public async Task QrScannerPanel_ShouldProcessValidQrCode()
    {
        // Arrange
        var mockRentalService = new Mock<IRentalService>();
        var testRental = CreateTestRental();
        mockRentalService.Setup(s => s.GetByQrCodeAsync(It.IsAny<string>()))
            .ReturnsAsync(testRental);
        
        var component = new QrScannerPanel(mockRentalService.Object);
        
        // Act
        await component.OnQrCodeDetected("TEST-QR-CODE");
        
        // Assert
        Assert.NotNull(component.CurrentRental);
        Assert.Equal(testRental.Id, component.CurrentRental.Id);
    }
}
```

---

## Performance Optimization

### Component Optimization

1. **Lazy Loading**: Load data only when needed
2. **Virtual Scrolling**: For large equipment lists
3. **Debouncing**: Search input debouncing
4. **Memoization**: Cache expensive calculations
5. **Component Splitting**: Break large components into smaller ones

### Memory Management

```csharp
// Implement IDisposable for cleanup
public class BookingWizard : ComponentBase, IDisposable
{
    private Timer? refreshTimer;
    private CancellationTokenSource? cts;
    
    protected override async Task OnInitializedAsync()
    {
        cts = new CancellationTokenSource();
        refreshTimer = new Timer(async _ => 
        {
            if (!cts.Token.IsCancellationRequested)
            {
                await InvokeAsync(RefreshData);
            }
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }
    
    public void Dispose()
    {
        refreshTimer?.Dispose();
        cts?.Cancel();
        cts?.Dispose();
    }
}
```

---

## Accessibility Features

### ARIA Support

```razor
<div class="wizard-step" role="tabpanel" aria-labelledby="step-@currentStep-title">
    <h2 id="step-@currentStep-title">@GetStepTitle(currentStep)</h2>
    
    <form aria-label="@GetFormLabel(currentStep)">
        <div class="form-group">
            <label for="height-input" class="form-label">Wzrost (cm)</label>
            <InputNumber id="height-input" 
                        @bind-Value="profileData.Height"
                        aria-describedby="height-help"
                        aria-invalid="@validationErrors.ContainsKey(nameof(profileData.Height))" />
            <div id="height-help" class="form-help">
                Podaj wzrost w centymetrach (100-250 cm)
            </div>
            <ValidationMessage For="@(() => profileData.Height)" />
        </div>
    </form>
</div>
```

### Keyboard Navigation

```css
.equipment-card:focus {
    outline: 3px solid #667eea;
    outline-offset: 2px;
}

.rating-stars button:focus {
    outline: 3px solid #667eea;
    transform: scale(1.1);
}

.step-indicator:focus {
    outline: 3px solid white;
    outline-offset: 2px;
}
```

---

## Future Enhancements

### Planned Features

1. **Progressive Web App**: Offline capabilities
2. **Real-time Updates**: SignalR integration
3. **Advanced Charts**: Interactive data visualization
4. **Mobile Optimization**: Touch-friendly interfaces
5. **Internationalization**: Multi-language support

### Component Roadmap

- **v2.1**: Enhanced booking wizard with saved profiles
- **v2.2**: Advanced analytics with predictive insights
- **v2.3**: Mobile-first employee interface
- **v2.4**: Voice commands and accessibility improvements
- **v3.0**: AI-powered recommendations and chat integration

---

## Version History

- **v1.0**: Initial component library
- **v1.1**: Added booking wizard
- **v1.2**: Employee QR scanner panel
- **v1.3**: Analytics dashboard
- **v1.4**: Enhanced styling and accessibility
- **v1.5**: Performance optimizations
