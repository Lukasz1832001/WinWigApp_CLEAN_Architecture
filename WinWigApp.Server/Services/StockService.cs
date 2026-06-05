using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Services;

public class StockService : IStockService
{
    // Statyczna lista spółek z YahooFinanceClient.SymbolMap
    // Ceny i wolumeny pobierane są z Yahoo Finance w czasie rzeczywistym.
    private static readonly List<(string Symbol, string Name)> WIG20_META =
    [
        ("ALLEGRO",   "Allegro"),
        ("ALIOR",     "Alior Bank"),
        ("BUDIMEX",   "Budimex"),
        ("CDPROJEKT", "CD Projekt"),
        ("DINOPL",    "Dino Polska"),
        ("ERSTEPL",   "Erste Bank"),
        ("KGHM",      "KGHM Polska Miedź"),
        ("KRUK",      "KRUK"),
        ("KETY",      "Kęty"),
        ("LPP",       "LPP"),
        ("MBANK",     "mBank"),
        ("MODIVO",    "Modivo"),
        ("PEPCO",     "Pepco"),
        ("PEKAO",     "Bank Pekao"),
        ("PGE",       "PGE Polska Grupa Energetyczna"),
        ("PKNORLEN",  "PKN Orlen"),
        ("PKOBP",     "PKO Bank Polski"),
        ("PZU",       "PZU"),
        ("TAURONPE",  "Tauron Polska Energia"),
        ("ZABKA",     "Żabka Polska"),
    ];

    private readonly IYahooFinanceClient _yahoo;
    private readonly ILogger<StockService> _logger;

    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static List<StockResponse>? _cachedQuotes;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public StockService(IYahooFinanceClient yahoo, ILogger<StockService> logger)
    {
        _yahoo = yahoo;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // GET /api/stocks
    public async Task<List<StockResponse>> GetStocksAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_cachedQuotes != null && DateTime.UtcNow < _cacheExpiry)
                return _cachedQuotes;

            using var throttle = new SemaphoreSlim(5);
            var tasks = WIG20_META.Select(async meta =>
            {
                await throttle.WaitAsync();
                try { return await FetchStockResponseAsync(meta.Symbol, meta.Name); }
                finally { throttle.Release(); }
            });

            _cachedQuotes = [.. await Task.WhenAll(tasks)];
            _cacheExpiry = DateTime.UtcNow + CacheDuration;
            return _cachedQuotes;
        }
        finally
        {
            _lock.Release();
        }
    }

    // -------------------------------------------------------------------------
    // GET /api/stocks/{symbol}/candlestick
    public async Task<List<CandlestickData>> GetCandlestickDataAsync(string symbol, int days)
    {
        var candles = await _yahoo.GetHistoricalDataAsync(symbol, days);

        if (candles.Count == 0)
        {
            _logger.LogWarning("Brak danych świecowych dla {Symbol} — używam fallbacku syntetycznego", symbol);
            candles = GenerateSyntheticCandles(100m, days);
        }

        return candles;
    }

    // -------------------------------------------------------------------------
    // GET /api/stocks/{symbol}/technical
    public async Task<TechnicalIndicatorsResponse> GetTechnicalIndicatorsAsync(string symbol, int days)
    {
        int fetchDays = Math.Max(days + 200, 252);
        var candles = await _yahoo.GetHistoricalDataAsync(symbol, fetchDays);

        if (candles.Count == 0)
        {
            _logger.LogWarning("Brak danych technicznych dla {Symbol} — używam fallbacku syntetycznego", symbol);
            candles = GenerateSyntheticCandles(100m, fetchDays);
        }

        return CalculateTechnicalIndicators(candles, days);
    }


    private async Task<StockResponse> FetchStockResponseAsync(string symbol, string name)
    {
        var quote = await _yahoo.GetQuoteAsync(symbol);

        if (quote == null)
        {
            _logger.LogWarning("Brak notowania dla {Symbol} — zwracam zerowe wartości", symbol);
            return new StockResponse { Symbol = symbol, Name = name };
        }

        return new StockResponse
        {
            Symbol = symbol,
            Name = name,
            CurrentPrice = quote.CurrentPrice,
            Volume = quote.Volume,
            OpenPrice = quote.OpenPrice,
            ClosePrice = quote.ClosePrice,
            Change = quote.Change,
            ChangePercent = quote.ChangePercent,
            PeRatio = 0,
            PbRatio = 0,
            Roe = 0,
        };
    }


    // Wskaźniki techniczne

    private static TechnicalIndicatorsResponse CalculateTechnicalIndicators(
        List<CandlestickData> candles, int requestedDays)
    {
        var closes = candles.Select(c => c.Close).ToList();

        return new TechnicalIndicatorsResponse
        {
            Rsi = Trim(CalculateRSI(closes), requestedDays).ToArray(),
            Macd = Trim(CalculateMACD(closes), requestedDays).ToArray(),
            Sma50 = Trim(CalculateSMA(closes, 50), requestedDays).ToArray(),
            Sma200 = Trim(CalculateSMA(closes, 200), requestedDays).ToArray(),
        };
    }

    private static List<T> Trim<T>(List<T> list, int count) =>
        list.Count <= count ? list : list.TakeLast(count).ToList();

    // RSI (14 okresów, Wilder smoothing uproszczony do prostej średniej)
    private static List<decimal> CalculateRSI(List<decimal> closes, int period = 14)
    {
        var rsi = new List<decimal>(closes.Count);
        for (int i = 0; i < closes.Count; i++)
        {
            if (i < period) { rsi.Add(50); continue; }

            decimal gain = 0, loss = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                var d = closes[j] - closes[j - 1];
                if (d > 0) gain += d; else loss += Math.Abs(d);
            }
            gain /= period;
            loss /= period;
            rsi.Add(loss == 0 ? 100 : 100 - 100 / (1 + gain / loss));
        }
        return rsi;
    }

    // MACD (12, 26, 9)
    private static List<MacdIndicator> CalculateMACD(List<decimal> closes)
    {
        var ema12 = CalculateEMA(closes, 12);
        var ema26 = CalculateEMA(closes, 26);
        var macdLine = ema12.Select((v, i) => v - ema26[i]).ToList();
        var signal = CalculateEMA(macdLine, 9);

        return macdLine.Select((v, i) => new MacdIndicator
        {
            Value = v,
            Signal = signal[i],
            Histogram = v - signal[i],
        }).ToList();
    }

    // SMA
    private static List<decimal> CalculateSMA(List<decimal> closes, int period)
    {
        var sma = new List<decimal>(closes.Count);
        for (int i = 0; i < closes.Count; i++)
        {
            if (i < period - 1) { sma.Add(closes[i]); continue; }
            decimal sum = 0;
            for (int j = i - period + 1; j <= i; j++) sum += closes[j];
            sma.Add(sum / period);
        }
        return sma;
    }

    // EMA
    private static List<decimal> CalculateEMA(List<decimal> data, int period)
    {
        if (data.Count == 0) return [];
        var k = 2m / (period + 1);
        var ema = new List<decimal>(data.Count) { data[0] };
        for (int i = 1; i < data.Count; i++)
            ema.Add(data[i] * k + ema[i - 1] * (1 - k));
        return ema;
    }

    // dane syntetyczne gdy Yahoo nie odpowiada (weekend, awaria)

    private static List<CandlestickData> GenerateSyntheticCandles(decimal basePrice, int days)
    {
        var list = new List<CandlestickData>(days);
        decimal p = basePrice * 0.9m;
        var rng = new Random(42);

        for (int i = 0; i < days; i++)
        {
            var open = p;
            var chg = (decimal)(rng.NextDouble() - 0.48) * p * 0.03m;
            var close = open + chg;
            var high = Math.Max(open, close) * (1 + (decimal)rng.NextDouble() * 0.02m);
            var low = Math.Min(open, close) * (1 - (decimal)rng.NextDouble() * 0.02m);

            list.Add(new CandlestickData
            {
                Timestamp = new DateTimeOffset(DateTime.UtcNow.AddDays(-(days - i))).ToUnixTimeMilliseconds(),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = (long)(rng.NextDouble() * 2_000_000) + 500_000,
            });
            p = close;
        }
        return list;
    }
}