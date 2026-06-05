using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Services;

public interface IStrategyService
{
    Task<StrategyResponse> CreateStrategyAsync(Guid userId, CreateStrategyRequest request);
    Task<List<StrategyResponse>> GetUserStrategiesAsync(Guid userId);
    Task<StrategyResponse> GetStrategyByIdAsync(Guid strategyId, Guid userId);
    Task<BaseResponse> UpdateStrategyAsync(Guid strategyId, Guid userId, CreateStrategyRequest request);
    Task<BaseResponse> DeleteStrategyAsync(Guid strategyId, Guid userId);
    Task<ToggleStrategyResponse> ToggleStrategyAsync(Guid strategyId, Guid userId);
}
