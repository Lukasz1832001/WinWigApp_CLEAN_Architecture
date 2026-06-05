using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Services
{
    public interface IYahooFinanceClient
    {
        Task<YahooQuoteResult?> GetQuoteAsync(string symbol);
        Task<List<CandlestickData>> GetHistoricalDataAsync(string symbol, int days);
    }
}
