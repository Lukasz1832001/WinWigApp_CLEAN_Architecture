# Dokumentacja: Implementacja Logiki Wykonywania Strategii Inwestycyjnych

## Przegląd Zmian

Zaimplementowałem kompletną logikę dla systemu strategii inwestycyjnych, która umożliwia użytkownikowi:
1. Uruchamianie strategii inwestycyjnych
2. Obserwowanie spółek WIG20 w czasie rzeczywistym
3. Automatyczne generowanie powiadomień BUY/SELL na podstawie wskaźników technicznych
4. Zarządzanie powiadomieniami

## Nowe Komponenty

### 1. Domain - Nowe Encje

#### `WinWigApp.Domain\Entities\Notification.cs`
- **Enum `NotificationType`**: Buy, Sell
- **Klasa `Notification`**: Reprezentuje powiadomienie o sygnale handlowym
  - Przechowuje: UserId, StrategyId, Symbol, StockName, Message, Type, CreatedAt, IsRead

### 2. Infrastructure - Repository Pattern

#### `WinWigApp.Infrastructure\Repositories\INotificationRepository.cs`
- Interfejs definiujący operacje CRUD dla powiadomień
- Metody:
  - `GetUserNotificationsAsync(userId)` - pobranie wszystkich powiadomień użytkownika
  - `GetUnreadNotificationsAsync(userId)` - pobranie nieprzeczytanych
  - `GetNotificationsByStrategyAsync(strategyId)` - pobranie dla konkretnej strategii
  - `MarkAsReadAsync(notificationId)` - oznaczenie jako przeczytane
  - `MarkAllAsReadAsync(userId)` - oznaczenie wszystkich jako przeczytane
  - `DeleteOldNotificationsAsync(daysOld)` - usunięcie starych powiadomień

#### `WinWigApp.Infrastructure\Repositories\NotificationRepository.cs`
- Implementacja interface'u z wykorzystaniem GenericRepository
- Obsługuje wszystkie operacje na powiadomieniach

### 3. Application - Serwisy

#### `WinWigApp.Application\Services\StrategyExecutionService.cs`
**Główny serwis odpowiedzialny za analizę strategii**

**Interface `IStrategyExecutionService`:**
- `ExecuteStrategyAsync(strategyId, userId)` - uruchomienie analizy dla konkretnej strategii
- `ExecuteAllActiveStrategiesAsync()` - uruchomienie analizy dla wszystkich aktywnych strategii

**Klasa `StrategyExecutionService`:**
- Analizuje wskaźniki techniczne: RSI, MACD, SMA50/SMA200
- Sprawdza warunki strategii dla każdej spółki WIG20
- Generuje powiadomienia BUY/SELL na podstawie warunków

**Logika sygnałów:**

**Sygnał BUY** (spełnione warunki):
- RSI < RsiHigh (spółka nie jest wykupiona)
- Jeśli MacdBuy=true: Histogram MACD > 0 (wskaźnik wzrostu)
- Jeśli Sma50Above200=true: SMA50 > SMA200 (trend wzrostu)

**Sygnał SELL** (spełnione warunki):
- RSI > RsiLow (spółka jest wykupiona)
- Jeśli MacdBuy=true: Histogram MACD <= 0 (wskaźnik spadku)
- Jeśli Sma50Above200=true: SMA50 <= SMA200 (trend spadku)

#### `WinWigApp.Application\Services\StrategyExecutionBackgroundService.cs`
**BackgroundService do cyklicznej analizy**
- Uruchomi się automatycznie przy starcie aplikacji
- Czeka 30 sekund na inicjalizację
- Analizuje strategie co 5 minut
- Uruchamia `ExecuteAllActiveStrategiesAsync()`
- Graceful shutdown

#### `WinWigApp.Application\Services\NotificationService.cs`
**Serwis do zarządzania powiadomieniami**

**Interface `INotificationService`:**
- `GetUserNotificationsAsync(userId)` - pobranie powiadomień użytkownika
- `GetUnreadNotificationsAsync(userId)` - pobranie nieprzeczytanych
- `MarkAsReadAsync(notificationId, userId)` - zaznaczenie jako przeczytane
- `MarkAllAsReadAsync(userId)` - zaznaczenie wszystkich jako przeczytane
- `DeleteNotificationAsync(notificationId, userId)` - usunięcie powiadomienia
- `CleanupOldNotificationsAsync()` - usunięcie powiadomień starszych niż 30 dni

### 4. API Controllers

#### `WinWigApp.Server\Controllers\NotificationsController.cs`
**Endpoints do zarządzania powiadomieniami:**
- `GET /api/notifications` - pobranie wszystkich powiadomień
- `GET /api/notifications/unread` - pobranie nieprzeczytanych
- `PUT /api/notifications/{id}/read` - zaznaczenie jako przeczytane
- `PUT /api/notifications/read-all` - zaznaczenie wszystkich jako przeczytane
- `DELETE /api/notifications/{id}` - usunięcie powiadomienia

### 5. DTOs

#### `WinWigApp.Application\DTOs\NotificationResponse.cs`
```csharp
public class NotificationResponse
{
	public Guid Id { get; set; }
	public Guid StrategyId { get; set; }
	public string Symbol { get; set; }
	public string StockName { get; set; }
	public string Message { get; set; }
	public string Type { get; set; } // "Buy" lub "Sell"
	public DateTime CreatedAt { get; set; }
	public bool IsRead { get; set; }
}
```

## Przepływ Działania

### 1. Użytkownik Uruchamia Strategię
```
Użytkownik klika "Uruchom" w UI
↓
POST /api/strategies/{id}/toggle
↓
ToggleStrategyAsync(strategyId, userId)
↓
Strategy.IsActive = true
↓
ExecuteStrategyAsync(strategyId, userId) - pierwsze uruchomienie
```

### 2. BackgroundService - Cykliczna Analiza
```
Aplikacja startuje
↓
StrategyExecutionBackgroundService uruchamia się
↓
Co 5 minut: ExecuteAllActiveStrategiesAsync()
↓
Dla każdej aktywnej strategii: ExecuteStrategyAsync()
↓
Analiza wszystkich spółek WIG20
```

### 3. Analiza Spółki
```
AnalyzeStockForStrategyAsync(strategy, stock, userId)
↓
Pobranie wskaźników technicznych (RSI, MACD, SMA)
↓
CheckBuySignal() - czy warunki BUY są spełnione?
↓
CheckSellSignal() - czy warunki SELL są spełnione?
↓
Sprawdzenie Portfolio użytkownika
  - Brak akcji → możliwy tylko BUY
  - Ma akcje → możliwy BUY lub SELL
↓
CreateNotificationAsync() - utworzenie powiadomienia
```

### 4. Tworzenie Powiadomienia
```
CreateNotificationAsync(userId, strategyId, symbol, message, type)
↓
Sprawdzenie duplikatów (ostatnie 30 minut)
↓
Jeśli nie istnieje: Utwórz nowe powiadomienie
↓
Zapisz w bazie danych
```

## Reguły Powiadomień

### Dla użytkownika BEZ akcji spółki w portfelu
- **Tylko sygnał BUY** może generować powiadomienie
- Wiadomość: `"Kupno: Spółka {Name} ({Symbol}) spełnia warunki strategii {Strategy}"`

### Dla użytkownika MAJĄCEGO akcje spółki w portfelu
- **Sygnał BUY** - wiadomość: `"Kupno: Spółka {Name} ({Symbol}) spełnia warunki strategii {Strategy}"`
- **Sygnał SELL** - wiadomość: `"Sprzedaż: Spółka {Name} ({Symbol}) spełnia warunki wyjścia ze strategii {Strategy}"`

## Systemy Informacyjne

### Wskaźniki Techniczne
1. **RSI (Relative Strength Index)** - okres 14
   - Wartość 0-100
   - RsiLow < RsiHigh

2. **MACD (Moving Average Convergence Divergence)**
   - Histogram MACD porównywany z zerem
   - Wskazuje kierunek trendu

3. **SMA (Simple Moving Average)**
   - SMA50 - średnia 50 okresów
   - SMA200 - średnia 200 okresów
   - Porównanie: trend wzrostu (SMA50 > SMA200)

## Aktualizacje Bazy Danych

### DbContext
- Dodano `DbSet<Notification> Notifications`
- Konfiguracja relacji: Notification → User (1:N)
- Konfiguracja relacji: Notification → Strategy (1:N)
- OnDelete: Cascade

### UnitOfWork
- Dodano `INotificationRepository Notifications`
- Implementacja lazy-loading w konstruktorze

## Rejestracja Serwisów (Program.cs)

```csharp
builder.Services.AddScoped<IStrategyExecutionService, StrategyExecutionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<StrategyExecutionBackgroundService>();
```

## Zmiany w Istniejących Komponentach

### User Entity
- Dodano: `public ICollection<Notification> Notifications { get; set; } = [];`

### Strategy Entity
- Dodano: `public ICollection<Notification> Notifications { get; set; } = [];`

### StrategyService
- Dodano: `IStrategyExecutionService` w konstruktorze
- W `ToggleStrategyAsync`: Po aktywacji strategii → uruchomienie analizy

### MappingProfile
- Dodano mapowanie: `Notification → NotificationResponse`

### Application.csproj
- Dodano: `Microsoft.Extensions.Hosting.Abstractions` v9.0.0
- Dodano: `Microsoft.Extensions.DependencyInjection` v9.0.0

## Testy Funkcjonalne

### Przypadek 1: Użytkownik BEZ akcji, Sygnał BUY
```
1. Użytkownik tworzy strategię z warunkami
2. Uruchamia strategię
3. BackgroundService analizuje spółkę
4. Warunki BUY spełnione
5. Portfolio jest pusty dla tej spółki
6. ✅ Powiadomienie BUY utworzone
```

### Przypadek 2: Użytkownik MA akcje, Sygnał SELL
```
1. Użytkownik ma akcje KGHM w portfelu
2. Uruchamia strategię
3. BackgroundService analizuje spółkę
4. Warunki SELL spełnione
5. Portfolio zawiera KGHM
6. ✅ Powiadomienie SELL utworzone
```

### Przypadek 3: Duplikaty
```
1. Pierwszy sygnał BUY w 12:00 → Powiadomienie 1
2. Drugi sygnał BUY w 12:02 (ta sama spółka, strategia, typ)
3. Sprawdzenie: ostatnie 30 minut
4. ✅ Powiadomienie 2 nie będzie utworzone (duplikat)
```

## Performance

- **Interwał analizy**: 5 minut
- **Początkowe opóźnienie**: 30 sekund
- **Liczba spółek**: 20 (WIG20)
- **Baza danych**: SQLite (nie blokuje)

## Instrukcja Konfiguracji

### 1. Zmiana interwału analizy
Plik: `StrategyExecutionBackgroundService.cs`
```csharp
private readonly TimeSpan _executionInterval = TimeSpan.FromMinutes(5); // Zmień tutaj
```

### 2. Zmiana okresu przechowywania powiadomień
Plik: `NotificationService.cs` - metoda `CleanupOldNotificationsAsync()`
```csharp
await _unitOfWork.Notifications.DeleteOldNotificationsAsync(daysOld: 30); // Zmień tutaj
```

### 3. Zmiana czasu inicjalizacji BackgroundService
Plik: `StrategyExecutionBackgroundService.cs`
```csharp
await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Zmień tutaj
```

## Logging

Serwis loguje:
- Rozpoczęcie/zakończenie analizy strategii
- Uruchamianie BackgroundService
- Tworzenie powiadomień
- Błędy i ostrzeżenia

Logi dostępne w: Debug Output → Application

## Przyszłe Ulepszenia

1. **Push Notifications** - powiadomienia na urządzeniu
2. **Email Notifications** - wysyłka na email
3. **Custom Intervals** - różne interwały dla różnych strategii
4. **Alert Thresholds** - progi alertów
5. **Backtesting** - testowanie strategii na danych historycznych
6. **Machine Learning** - optymalizacja warunków strategii

---

**Autor**: GitHub Copilot
**Data**: 2026-06-05
**Wersja**: 1.0
