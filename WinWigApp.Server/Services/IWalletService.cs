using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Services
{
    public interface IWalletService
    {
        Task<DepositResponse> DepositAsync(Guid userId, DepositRequest request);
        Task<List<DepositsResponse>> GetDepositsAsync(Guid userId);
        Task<BalanceResponse> GetBalanceAsync(Guid userId);
    }
}
