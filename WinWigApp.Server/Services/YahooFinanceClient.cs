using System.Text.Json;
using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Services;

public class YahooFinanceClient : IYahooFinanceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooFinanceClient> _logger;

     // Mapowanie symboli GPW → ticker Yahoo Finance
    public static readonly Dictionary<string, string> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ALLEGRO",   "ALE.WA" },
        { "ALIOR",     "ALR.WA" },
        { "BUDIMEX",   "BDX.WA" },
        { "CDPROJEKT", "CDR.WA" },
        { "DINOPL",    "DNP.WA" },
        { "ERSTEPL",   "EBP.WA" }, 
        { "KGHM",      "KGH.WA" },
        { "KRUK",      "KRU.WA" },
        { "KETY",      "KTY.WA" },
        { "LPP",       "LPP.WA" },
        { "MBANK",     "MBK.WA" },
        { "MODIVO",    "MDV.WA" },
        { "PEPCO",     "PCO.WA" },
        { "PEKAO",     "PEO.WA" },
        { "PGE",       "PGE.WA" },
        { "PKNORLEN",  "PKN.WA" }, 
        { "PKOBP",     "PKO.WA" },
        { "PZU",       "PZU.WA" },
        { "TAURONPE",  "TPE.WA" },
        { "ZABKA",     "ZAB.WA" }
    };

    public YahooFinanceClient(HttpClient http, ILogger<YahooFinanceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    private string ToYahooSymbol(string symbol) =>
        SymbolMap.TryGetValue(symbol, out var y) ? y : $"{symbol}.WA";

    public async Task<YahooQuoteResult?> GetQuoteAsync(string symbol)
    {
        var yahooSymbol = ToYahooSymbol(symbol);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?interval=1d&range=1d";

        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Finance zwrócił {Status} dla {Symbol}", response.StatusCode, yahooSymbol);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var chart = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var meta = chart.GetProperty("meta");

            decimal currentPrice = GetDecimal(meta, "regularMarketPrice") ?? 0;
            decimal prevClose = GetDecimal(meta, "chartPreviousClose")
                                ?? GetDecimal(meta, "previousClose")
                                ?? currentPrice;
            decimal open = GetDecimal(meta, "regularMarketOpen") ?? currentPrice;
            decimal dayLow = GetDecimal(meta, "regularMarketDayLow") ?? currentPrice;
            decimal dayHigh = GetDecimal(meta, "regularMarketDayHigh") ?? currentPrice;
            long volume = GetLong(meta, "regularMarketVolume") ?? 0;
            decimal change = currentPrice - prevClose;
            decimal changePct = prevClose != 0 ? (change / prevClose) * 100 : 0;

            return new YahooQuoteResult
            {
                CurrentPrice = currentPrice,
                OpenPrice = open,
                ClosePrice = prevClose,
                DayLow = dayLow,
                DayHigh = dayHigh,
                Volume = volume,
                Change = change,
                ChangePercent = changePct,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania notowania {Symbol} z Yahoo Finance", yahooSymbol);
            return null;
        }
    }

    public async Task<List<CandlestickData>> GetHistoricalDataAsync(string symbol, int days)
    {
        var yahooSymbol = ToYahooSymbol(symbol);
        // Yahoo: dla 1 dnia używamy interwału 5m, dla reszty 1d
        string interval = days == 1 ? "5m" : "1d";
        // Dodaj margines na weekendy/święta
        int fetchDays = days == 1 ? 1 : (int)Math.Ceiling(days * 1.5);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}" +
                  $"?interval={interval}&range={fetchDays}d";

        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Finance zwrócił {Status} dla historii {Symbol}", response.StatusCode, yahooSymbol);
                return [];
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var result = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var timestamps = result.GetProperty("timestamp").EnumerateArray().Select(t => t.GetInt64()).ToList();
            var quote = result.GetProperty("indicators").GetProperty("quote")[0];

            var opens = GetDecimalArray(quote, "open");
            var highs = GetDecimalArray(quote, "high");
            var lows = GetDecimalArray(quote, "low");
            var closes = GetDecimalArray(quote, "close");
            var volumes = GetLongArray(quote, "volume");

            var candles = new List<CandlestickData>();
            for (int i = 0; i < timestamps.Count; i++)
            {
                // Pomiń świece z null wartościami (brak handlu)
                if (opens[i] == null || closes[i] == null) continue;

                candles.Add(new CandlestickData
                {
                    Timestamp = timestamps[i] * 1000, // ms
                    Open = opens[i]!.Value,
                    High = highs[i] ?? closes[i]!.Value,
                    Low = lows[i] ?? closes[i]!.Value,
                    Close = closes[i]!.Value,
                    Volume = volumes[i] ?? 0,
                });
            }

            // Przytnij do żądanej liczby dni (od końca)
            if (days > 1 && candles.Count > days)
                candles = candles.TakeLast(days).ToList();

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania historii {Symbol} z Yahoo Finance", yahooSymbol);
            return [];
        }
    }

    // --- Helpers ---

    private static decimal? GetDecimal(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetDecimal();
        return null;
    }

    private static long? GetLong(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetInt64();
        return null;
    }

    private static List<decimal?> GetDecimalArray(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var arr)) return [];
        return arr.EnumerateArray()
                  .Select(v => v.ValueKind == JsonValueKind.Number ? (decimal?)v.GetDecimal() : null)
                  .ToList();
    }

    private static List<long?> GetLongArray(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var arr)) return [];
        return arr.EnumerateArray()
                  .Select(v => v.ValueKind == JsonValueKind.Number ? (long?)v.GetInt64() : null)
                  .ToList();
    }
}

/// <summary>DTO z wynikiem notowania z Yahoo Finance</summary>
public class YahooQuoteResult
{
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal DayLow { get; set; }
    public decimal DayHigh { get; set; }
    public long Volume { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
}