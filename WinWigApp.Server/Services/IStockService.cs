using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Services
{
    public interface IStockService
    {
        Task<List<StockResponse>> GetStocksAsync();
        Task<List<CandlestickData>> GetCandlestickDataAsync(string symbol, int days);
        Task<TechnicalIndicatorsResponse> GetTechnicalIndicatorsAsync(string symbol, int days);
    }
}
