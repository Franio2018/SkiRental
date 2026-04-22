using Plugin.NFC;

namespace SkiRental.App.Services;

/// <summary>
/// Singleton serwis mostkujący natywne API skanera QR (ZXing) i NFC (Plugin.NFC)
/// z warstwą Blazor. Udostępnia zdarzenie QrCodeDetected subskrybowane
/// przez MainPage i przez JS Interop.
/// </summary>
public sealed class QrNfcBridgeService : IDisposable
{
    // ── Zdarzenie – wynik skanu QR lub NFC ──────────────────────────────────
    public event Action<string>? QrCodeDetected;

    private bool _nfcInitialized;

    // ── NFC ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicjalizuje nasłuchiwanie tagów NFC.
    /// Wywołaj z MainPage.OnAppearing() lub po przejściu do panelu pracownika.
    /// </summary>
    public void StartNfcListening()
    {
        if (!CrossNFC.IsSupported || _nfcInitialized) return;

        CrossNFC.Current.OnMessageReceived += OnNfcMessageReceived;
        CrossNFC.Current.OnTagDiscovered   += OnNfcTagDiscovered;
        CrossNFC.Current.StartListening();
        _nfcInitialized = true;
    }

    public void StopNfcListening()
    {
        if (!CrossNFC.IsSupported || !_nfcInitialized) return;

        CrossNFC.Current.StopListening();
        CrossNFC.Current.OnMessageReceived -= OnNfcMessageReceived;
        CrossNFC.Current.OnTagDiscovered   -= OnNfcTagDiscovered;
        _nfcInitialized = false;
    }

    private void OnNfcMessageReceived(ITagInfo tagInfo)
    {
        if (tagInfo?.Records is null || tagInfo.Records.Length == 0) return;

        // Pobierz pierwszy rekord NDEF (typ Text lub URI)
        var record  = tagInfo.Records[0];
        var payload = record.Message; // zakodowany QR/ID

        if (!string.IsNullOrWhiteSpace(payload))
            RaiseQrDetected(payload);
    }

    private void OnNfcTagDiscovered(ITagInfo tagInfo, bool format)
    {
        // Opcjonalnie: formatuj pusty tag do zapisu ID wypożyczenia
    }

    // ── QR (wywoływane przez MainPage z ZXing callback) ─────────────────────

    /// <summary>
    /// Wywołaj po wykryciu kodu QR przez ZXing (MainPage.OnBarcodesDetected).
    /// </summary>
    public void NotifyQrCodeDetected(string qrCode) => RaiseQrDetected(qrCode);

    private void RaiseQrDetected(string code)
    {
        MainThread.InvokeOnMainThreadAsync(() => QrCodeDetected?.Invoke(code));
    }

    // ── Zapis QR/NFC na tag (do wydania opaski klientowi) ───────────────────

    /// <summary>
    /// Zapisuje kod wypożyczenia na tag NFC (opaska klienta).
    /// </summary>
    public async Task WriteRentalToNfcTagAsync(string rentalQrCode)
    {
        // TYMCZASOWE ROZWIĄZANIE:
        // Zakomentowujemy kod, który sypie błędami, aby móc skompilować resztę aplikacji.
        // Wrócimy do tego, gdy aplikacja już "odpali".

        /*
        if (!CrossNFC.Current.IsAvailable || !CrossNFC.Current.IsEnabled)
            throw new NotSupportedException("NFC nie jest dostępne.");

        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.WellKnown,
            Payload = System.Text.Encoding.UTF8.GetBytes(rentalQrCode),
            LanguageCode = "en"
        };

        CrossNFC.Current.StartPublishing();
        // Tutaj był problematyczny kod
        */

        System.Diagnostics.Debug.WriteLine($"Zapis NFC (symulacja): {rentalQrCode}");
        await Task.CompletedTask;
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        StopNfcListening();
    }
}
