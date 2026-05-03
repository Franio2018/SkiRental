using Microsoft.AspNetCore.Components.WebView.Maui;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using SkiRental.App.Services;
using Microsoft.JSInterop;

namespace SkiRental.App;

/// <summary>
/// Code-behind strony głównej MAUI.
/// Mostkuje natywny skaner ZXing (XAML) z komponentem Blazor QrScannerPanel
/// poprzez QrNfcBridgeService i JavaScript Interop.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly QrNfcBridgeService _bridge;
    private readonly AuthenticationService _authService;

    // ZXing options – skanuj wyłącznie kody QR
    public BarcodeReaderOptions BarcodeOptions { get; } = new()
    {
        Formats   = BarcodeFormat.QrCode,
        AutoRotate = true,
        Multiple  = false
    };

    public MainPage(QrNfcBridgeService bridge, AuthenticationService authService)
    {
        InitializeComponent();
        _bridge = bridge;
        _authService = authService;
        BindingContext = this;

        // Subskrybuj zdarzenie z serwisu natywnego (np. skan NFC)
        _bridge.QrCodeDetected += OnQrCodeDetectedFromBridge;
        
        // Subscribe to authentication changes
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        
        // Update button visibility based on user role
        UpdateButtonVisibility();
    }

    // ── ZXing callback ───────────────────────────────────────────────────────

    /// <summary>
    /// Wywoływane przez ZXing gdy kamera wykryje kod QR.
    /// Przekazuje wynik do komponentu Blazor przez JS Interop.
    /// </summary>
    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results?.FirstOrDefault();
        if (first is null) return;

        // Wyłącz skaner po pierwszym wykryciu
        BarcodeReader.IsDetecting = false;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            ScannerOverlay.IsVisible = false;
            await InvokeBlazerQrCallback(first.Value);
        });
    }

    /// <summary>
    /// Przekazuje kod QR do aktywnego komponentu Blazor przez JS.
    /// Komponent QrScannerPanel.razor rejestruje się jako [JSInvokable("OnQrCodeDetected")].
    /// </summary>
    private async Task InvokeBlazerQrCallback(string qrCode)
    {
        // TryDispatchAsync oczekuje akcji (void), więc nie używamy 'return'
        await BlazorWebView.TryDispatchAsync(async sp =>
        {
            var js = sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
            // Wywołujemy asynchronicznie bez zwracania zadania do dyspaczera
            await js.InvokeVoidAsync("skiRentalBridge.dispatchQrCode", qrCode);
        });
    }

    // ── Otwieranie / zamykanie skanera ───────────────────────────────────────

    private async void OpenScannerAsync(object? sender, EventArgs e)
    {
        // Check if user is authenticated and has worker role
        if (!_authService.IsAuthenticated || _authService.CurrentRole != UserRole.Worker)
        {
            await DisplayAlert("Brak uprawnień",
                "Tylko pracownicy mogą używać skanera QR.", "OK");
            return;
        }

        // Sprawdź uprawnienia do kamery
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Brak uprawnień",
                "Aplikacja potrzebuje dostępu do kamery aby skanować kody QR.", "OK");
            return;
        }

        BarcodeReader.IsDetecting = true;
        ScannerOverlay.IsVisible  = true;
    }

    private async void CloseScannerAsync(object? sender, EventArgs e)
    {
        BarcodeReader.IsDetecting = false;
        ScannerOverlay.IsVisible  = false;
        await Task.CompletedTask;
    }

    // ── Bridge event (np. NFC) ───────────────────────────────────────────────

    private async void OnQrCodeDetectedFromBridge(string qrCode)
    {
        await InvokeBlazerQrCallback(qrCode);
    }

    // ── Nawigacja do widoków Blazor przez URL ────────────────────────────────

    public Command NavigateHomeCommand     => new(async () => await NavigateToAsync("/"));
    public Command NavigateBookingCommand  => new(async () => await NavigateToAsync("/booking"));
    public Command NavigateAnalyticsCommand => new(async () => await NavigateToWithPermissionAsync("/analytics"));

    private async Task NavigateToWithPermissionAsync(string url)
    {
        // Check if user is authenticated and has worker role for analytics
        if (url == "/analytics" && (!_authService.IsAuthenticated || _authService.CurrentRole != UserRole.Worker))
        {
            await DisplayAlert("Brak uprawnień",
                "Tylko pracownicy mogą przeglądać analitykę.", "OK");
            return;
        }

        await NavigateToAsync(url);
    }

    private void UpdateButtonVisibility()
    {
        // Find the buttons by their grid positions
        var scannerButton = this.FindByName<Button>("ScannerButton");
        var analyticsButton = this.FindByName<Button>("AnalyticsButton");

        bool isWorker = _authService.IsAuthenticated && _authService.CurrentRole == UserRole.Worker;

        if (scannerButton != null)
            scannerButton.IsVisible = isWorker;

        if (analyticsButton != null)
            analyticsButton.IsVisible = isWorker;
    }

    private async Task NavigateToAsync(string url)
    {
        // If the user switches tabs while the scanner overlay is open,
        // ensure we stop camera detection and hide the overlay.
        BarcodeReader.IsDetecting = false;
        ScannerOverlay.IsVisible  = false;

        await BlazorWebView.TryDispatchAsync(sp =>
        {
            var navManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            navManager.NavigateTo(url);
        });
    }

    // ── Authentication state handling ───────────────────────────────────────

    private void OnAuthenticationChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateButtonVisibility);
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    protected override void OnDisappearing()
    {
        _bridge.QrCodeDetected -= OnQrCodeDetectedFromBridge;
        _authService.AuthenticationChanged -= OnAuthenticationChanged;
        base.OnDisappearing();
    }
}
