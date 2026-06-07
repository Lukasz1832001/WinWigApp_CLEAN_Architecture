using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Services;

public class TechnicalRecommendationService : ITechnicalRecommendationService
{
    public RecommendationResult CalculateRecommendation(
        StockResponse stock,
        TechnicalIndicatorsResponse indicators
    )
    {
        int score = 0;
        var reasons = new List<string>();

        // Pobierz ostatnie wartości wskaźników
        decimal lastRsi = indicators.Rsi.Length > 0 
            ? indicators.Rsi[indicators.Rsi.Length - 1] 
            : 50;

        decimal lastMacdHistogram = indicators.Macd.Length > 0 
            ? indicators.Macd[indicators.Macd.Length - 1].Histogram 
            : 0;

        decimal lastSma50 = indicators.Sma50.Length > 0 
            ? indicators.Sma50[indicators.Sma50.Length - 1] 
            : 0;

        decimal lastSma200 = indicators.Sma200.Length > 0 
            ? indicators.Sma200[indicators.Sma200.Length - 1] 
            : 0;

        // RSI < 30 → score += 2 (Kup)
        if (lastRsi < 30)
        {
            score += 2;
            reasons.Add($"RSI ({lastRsi:F2}) < 30");
        }
        // RSI > 70 → score -= 2 (Sprzedaj)
        else if (lastRsi > 70)
        {
            score -= 2;
            reasons.Add($"RSI ({lastRsi:F2}) > 70");
        }

        // MACD Histogram > 0 → score += 1 (Kup)
        if (lastMacdHistogram > 0)
        {
            score += 1;
            reasons.Add($"MACD Histogram ({lastMacdHistogram:F4}) > 0");
        }
        // MACD Histogram < 0 → score -= 1 (Sprzedaj)
        else if (lastMacdHistogram < 0)
        {
            score -= 1;
            reasons.Add($"MACD Histogram ({lastMacdHistogram:F4}) < 0");
        }

        // SMA50 > SMA200 → score += 1 (Kup)
        if (lastSma50 > lastSma200 && lastSma200 > 0)
        {
            score += 1;
            reasons.Add($"SMA50 ({lastSma50:F2}) > SMA200 ({lastSma200:F2})");
        }
        // SMA50 < SMA200 → score -= 1 (Sprzedaj)
        else if (lastSma50 < lastSma200 && lastSma200 > 0)
        {
            score -= 1;
            reasons.Add($"SMA50 ({lastSma50:F2}) < SMA200 ({lastSma200:F2})");
        }

        // ChangePercent > 0 → score += 1 (Kup)
        if (stock.ChangePercent > 0)
        {
            score += 1;
            reasons.Add($"Zmiana ({stock.ChangePercent:F2}%) > 0");
        }
        // ChangePercent < 0 → score -= 1 (Sprzedaj)
        else if (stock.ChangePercent < 0)
        {
            score -= 1;
            reasons.Add($"Zmiana ({stock.ChangePercent:F2}%) < 0");
        }

        // Ustal rekomendację na podstawie wyniku
        string recommendation;
        if (score >= 2)
        {
            recommendation = "Kup";
        }
        else if (score <= -2)
        {
            recommendation = "Sprzedaj";
        }
        else
        {
            recommendation = "Czekaj";
        }

        return new RecommendationResult
        {
            Recommendation = recommendation,
            Score = score,
            Reason = string.Join("; ", reasons)
        };
    }
}