using Microsoft.Extensions.Logging;
using WinWigApp.Domain.Entities;
using WinWigApp.Infrastructure.UnitOfWork;
using WinWigApp.Application.Services;
using WinWigApp.Application.DTOs;
using AutoMapper;

namespace WinWigApp.Application.Services;

public interface IStrategyExecutionService
{
    Task ExecuteStrategyAsync(Guid strategyId, Guid userId);
    Task ExecuteAllActiveStrategiesAsync();
}

public class StrategyExecutionService : IStrategyExecutionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;
    private readonly INotificationHubClient _notificationHubClient;
    private readonly ILogger<StrategyExecutionService> _logger;
    private readonly IMapper _mapper;

    public StrategyExecutionService(IUnitOfWork unitOfWork, IStockService stockService, INotificationHubClient notificationHubClient, ILogger<StrategyExecutionService> logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _stockService = stockService;
        _notificationHubClient = notificationHubClient;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task ExecuteStrategyAsync(Guid strategyId, Guid userId)
    {
        try
        {
            var strategy = await _unitOfWork.Strategies.FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId)
                ?? throw new InvalidOperationException("Strategia nie znaleziona");

            if (!strategy.IsActive)
                return;

            _logger.LogInformation("Rozpoczynam wykonywanie strategii {StrategyId} dla użytkownika {UserId}", strategyId, userId);

            // Pobierz wszystkie spółki WIG20
            var stocks = await _stockService.GetStocksAsync();
            _logger.LogInformation("Pobrano {StockCount} spółek do analizy", stocks.Count);

            int buyCount = 0, sellCount = 0;

            // Analizuj każdą spółkę
            foreach (var stock in stocks)
            {
                var (hasBuy, hasSell) = await AnalyzeStockForStrategyAsync(strategy, stock, userId);
                if (hasBuy) buyCount++;
                if (hasSell) sellCount++;
            }

            _logger.LogInformation("Zakończono wykonywanie strategii {StrategyId} dla użytkownika {UserId}. Buy: {BuyCount}, Sell: {SellCount}", 
                strategyId, userId, buyCount, sellCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wykonywania strategii {StrategyId} dla użytkownika {UserId}", strategyId, userId);
        }
    }

    public async Task ExecuteAllActiveStrategiesAsync()
    {
        try
        {
            _logger.LogInformation("Rozpoczynam wykonywanie wszystkich aktywnych strategii");

            // Pobierz wszystkie aktywne strategie
            var activeStrategies = (await _unitOfWork.Strategies.FindAsync(s => s.IsActive)).ToList();

            foreach (var strategy in activeStrategies)
            {
                await ExecuteStrategyAsync(strategy.Id, strategy.UserId);
            }

            _logger.LogInformation("Zakończono wykonywanie wszystkich aktywnych strategii");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wykonywania wszystkich strategii");
        }
    }

    private async Task<(bool hasBuy, bool hasSell)> AnalyzeStockForStrategyAsync(Strategy strategy, StockResponse stock, Guid userId)
    {
        bool hasBuy = false, hasSell = false;
        try
        {
            // Pobierz wskaźniki techniczne
            var indicators = await _stockService.GetTechnicalIndicatorsAsync(stock.Symbol, 30);

            if (indicators.Rsi.Length == 0 || indicators.Macd.Length == 0 || indicators.Sma50.Length == 0)
            {
                _logger.LogDebug("Brak danych technicznych dla {Symbol}", stock.Symbol);
                return (false, false);
            }

            var lastRsi = indicators.Rsi[indicators.Rsi.Length - 1];
            var lastMacd = indicators.Macd[indicators.Macd.Length - 1];
            var lastSma50 = indicators.Sma50[indicators.Sma50.Length - 1];
            var lastSma200 = indicators.Sma200[indicators.Sma200.Length - 1];

            _logger.LogDebug("Analiza {Symbol}: RSI={RSI}, MACD={MACD}, SMA50={SMA50}, SMA200={SMA200}", 
                stock.Symbol, lastRsi, lastMacd.Histogram, lastSma50, lastSma200);

            // Sprawdź warunki strategii dla sygnału BUY
            bool buySignal = CheckBuySignal(strategy, lastRsi, lastMacd, lastSma50, lastSma200);

            // Sprawdź warunki strategii dla sygnału SELL
            bool sellSignal = CheckSellSignal(strategy, lastRsi, lastMacd, lastSma50, lastSma200);

            // Sprawdź czy użytkownik posiada akcje tej spółki
            var portfolio = await _unitOfWork.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == stock.Symbol);

            // Jeśli użytkownik nie posiada akcji, może być tylko sygnał BUY
            if (portfolio == null)
            {
                if (buySignal)
                {
                    _logger.LogInformation("BUY Signal dla {Symbol}: Spełnia kryteria strategii {StrategyName}", stock.Symbol, strategy.Name);
                    await CreateNotificationAsync(userId, strategy.Id, stock.Symbol, stock.Name, 
                        $"Kupno: Spółka {stock.Name} ({stock.Symbol}) spełnia warunki strategii {strategy.Name}", 
                        NotificationType.Buy);
                    hasBuy = true;
                }
                else
                {
                    // Log tylko - nie wysyłaj powiadomienia Wait (spam)
                    _logger.LogDebug("WAIT Signal dla {Symbol}: Warunki nie są jeszcze spełnione dla strategii {StrategyName}", stock.Symbol, strategy.Name);
                }
            }
            // Jeśli użytkownik posiada akcje, może być sygnał BUY lub SELL
            else
            {
                if (buySignal)
                {
                    _logger.LogInformation("BUY Signal dla {Symbol}: Spełnia kryteria strategii {StrategyName}", stock.Symbol, strategy.Name);
                    await CreateNotificationAsync(userId, strategy.Id, stock.Symbol, stock.Name,
                        $"Kupno: Spółka {stock.Name} ({stock.Symbol}) spełnia warunki strategii {strategy.Name}",
                        NotificationType.Buy);
                    hasBuy = true;
                }

                if (sellSignal)
                {
                    _logger.LogInformation("SELL Signal dla {Symbol}: Spełnia kryteria wyjścia ze strategii {StrategyName}", stock.Symbol, strategy.Name);
                    await CreateNotificationAsync(userId, strategy.Id, stock.Symbol, stock.Name,
                        $"Sprzedaż: Spółka {stock.Name} ({stock.Symbol}) spełnia warunki wyjścia ze strategii {strategy.Name}",
                        NotificationType.Sell);
                    hasSell = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Błąd podczas analizy spółki {Symbol} dla strategii {StrategyId}", stock.Symbol, strategy.Id);
        }

        return (hasBuy, hasSell);
    }

    private bool CheckBuySignal(Strategy strategy, decimal rsi, MacdIndicator macd, decimal sma50, decimal sma200)
    {
        // Warunek 1: RSI - powinno być poniżej RsiLow (przeceniona spółka - sygnał do kupna)
        if (rsi >= strategy.RsiLow)
            return false;

        // Warunek 2: MACD - jeśli strategia wymaga MACD buy sygnału, histogram powinien być dodatni (> 0)
        if (strategy.MacdBuy && macd.Histogram <= 0)
            return false;

        // Warunek 3: SMA - jeśli strategia wymaga, aby SMA50 było powyżej SMA200
        if (strategy.Sma50Above200 && sma50 <= sma200)
            return false;

        return true;
    }

    private bool CheckSellSignal(Strategy strategy, decimal rsi, MacdIndicator macd, decimal sma50, decimal sma200)
    {
        // Warunek 1: RSI - powinno być powyżej RsiHigh (wykupiona spółka - sygnał do sprzedaży)
        if (rsi <= strategy.RsiHigh)
            return false;

        // Warunek 2: MACD - jeśli histogram stał się ujemny
        if (strategy.MacdBuy && macd.Histogram > 0)
            return false;

        // Warunek 3: SMA - jeśli SMA50 spadło poniżej SMA200
        if (strategy.Sma50Above200 && sma50 >= sma200)
            return false;

        return true;
    }

    private async Task CreateNotificationAsync(Guid userId, Guid strategyId, string symbol, string stockName, string message, NotificationType type)
    {
        try
        {
            // Sprawdź czy takie powiadomienie już istnieje (aby uniknąć duplikatów)
            // Dla powiadomień Buy/Sell szukamy w ostatnie 24 godziny
            var existingNotification = await _unitOfWork.Notifications.FirstOrDefaultAsync(n =>
                n.UserId == userId &&
                n.StrategyId == strategyId &&
                n.Symbol == symbol &&
                n.Type == type &&
                n.CreatedAt > DateTime.UtcNow.AddMinutes(-1440)); // 24 godziny

            if (existingNotification != null)
            {
                _logger.LogDebug("Powiadomienie dla {Symbol} typu {Type} już istnieje", symbol, type);
                return;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StrategyId = strategyId,
                Symbol = symbol,
                StockName = stockName,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Utworzono powiadomienie dla użytkownika {UserId}: {Message}", userId, message);

            // Wyślij powiadomienie w czasie rzeczywistym do klienta
            if (_notificationHubClient != null)
            {
                try
                {
                    var notificationDto = _mapper.Map<NotificationResponse>(notification);
                    _logger.LogInformation("Wysyłanie powiadomienia przez SignalR do użytkownika {UserId}", userId);
                    await _notificationHubClient.SendNotificationAsync(userId, notificationDto);
                    _logger.LogInformation("Powiadomienie wysłane przez SignalR");
                }
                catch (Exception signalREx)
                {
                    _logger.LogWarning(signalREx, "Błąd przy wysyłaniu powiadomienia przez SignalR");
                    // Nie rzucaj błędu - powiadomienie zostało już zapisane w bazie
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia powiadomienia");
        }
    }
}
