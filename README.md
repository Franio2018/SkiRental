# SkiRental

System wypożyczalni nart biegowych napisany jako projekt indywidualny na studiach. Klient rezerwuje sprzęt w aplikacji, która dobiera długość nart, kijków i sztywność na podstawie wzrostu, wagi, poziomu i stylu jazdy. Pracownik wydaje i przyjmuje sprzęt po kodzie QR lub tagu NFC, a właściciel zarządza flotą, personelem i ma podgląd wykorzystania sprzętu.

Aplikacja to .NET MAUI z interfejsem w Blazorze (BlazorWebView), więc ten sam UI działa na Androidzie, iOS i Windowsie.

## Co umie

- **Dobór sprzętu** — długość nart i kijków oraz flex liczone ze wzrostu, BMI, poziomu zaawansowania i stylu (klasyczny / łyżwowy). Klient widzi, skąd wziął się wynik, i może podmienić pozycję na inną dostępną.
- **Rezerwacja** — kalendarz, przegląd dobranego sprzętu, płatność na miejscu albo online (PayU, BLIK), potwierdzenie z kodem QR.
- **Panel pracownika** — skan QR (ZXing) lub NFC przy wydaniu i zwrocie, ocena stanu sprzętu, dopłaty za spóźnienie i uszkodzenia.
- **Panel właściciela** — zarządzanie sprzętem, klientami, zamówieniami i personelem; wskaźnik wykorzystania sprzętu w sezonie z eksportem do CSV.
- **Konta i role** — logowanie z JWT i hasłami w BCrypt; role: klient, pracownik, właściciel.
- **Powiadomienia** — skrzynka w aplikacji oraz e-mail (MailKit) i SMS (Twilio).
- **Pogoda** — warunki na stoku z OpenWeatherMap na stronie głównej.

## Stack

C# / .NET 9, .NET MAUI + Blazor, EF Core 9 (SQLite, opcjonalnie SQL Server), ASP.NET Core Minimal API, PayU REST API, ZXing.Net.Maui, Plugin.NFC, xUnit + Moq + FluentAssertions.

## Struktura

```
SkiRental.Domain       encje, enumy, DTO
SkiRental.Logic        EF Core, repozytoria, logika biznesowa (rezerwacje, dobór sprzętu, PayU, analityka)
SkiRental.UI.Shared    współdzielone komponenty Blazor
SkiRental.App          aplikacja MAUI: strony, logowanie, QR/NFC, pogoda
SkiRental.Api          mały serwer na powiadomienia PayU (webhook) i health check
SkiRental.Tests        testy jednostkowe i integracyjne warstwy Logic
```

Aplikacja nie odpytuje API o dane — logika z `SkiRental.Logic` działa w procesie aplikacji. `SkiRental.Api` istnieje tylko po to, żeby PayU miało gdzie wysłać powiadomienie o płatności.

Szczegółowa dokumentacja (po angielsku) jest w [docs/](docs/README.md): architektura, schemat bazy, uwierzytelnianie.

## Uruchomienie

Potrzebne: .NET 9 SDK i workload MAUI (`dotnet workload install maui`).

```bash
dotnet restore
dotnet build SkiRental.App -f net9.0-windows10.0.19041.0   # albo net9.0-android
dotnet test SkiRental.Tests
```

Baza SQLite tworzy się przy pierwszym uruchomieniu i ma konta testowe:

| Login    | Hasło       | Rola       |
|----------|-------------|------------|
| `owner`  | `owner123`  | właściciel |
| `worker` | `worker123` | pracownik  |
| `user`   | `user123`   | klient     |

Klucze do PayU, OpenWeatherMap, SMTP i Twilio uzupełnia się w `SkiRental.App/appsettings.json`. W repo są tam tylko wartości zastępcze.
