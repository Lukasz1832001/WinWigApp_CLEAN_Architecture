using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Services
{
    public interface IYahooFinanceClient
    {
        Task<YahooQuoteResult?> GetQuoteAsync(string symbol);
        Task<List<CandlestickData>> GetHistoricalDataAsync(string symbol, int days);
    }
}
