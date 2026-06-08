using AutoMapper;
using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Models;
using Microsoft.Extensions.Logging;
using WinWigApp.Domain.Contracts;

namespace WinWigApp.Application.Services;

public class StrategyService : IStrategyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StrategyService> _logger;
    private readonly IMapper _mapper;
    private readonly IStrategyExecutionService _strategyExecutionService;
    private readonly INotificationHubClient _notificationHubClient;

    public StrategyService(IUnitOfWork unitOfWork, ILogger<StrategyService> logger, IMapper mapper, IStrategyExecutionService strategyExecutionService, INotificationHubClient notificationHubClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _strategyExecutionService = strategyExecutionService;
        _notificationHubClient = notificationHubClient;
    }

    public async Task<StrategyResponse> CreateStrategyAsync(Guid userId, CreateStrategyRequest request)
    {
        try
        {
            ValidateStrategyRequest(request);

            var strategy = new Strategy
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                TargetReturn = request.TargetReturn,
                InvestmentHorizon = request.InvestmentHorizon,
                RsiLow = request.RsiLow,
                RsiHigh = request.RsiHigh,
                MacdBuy = request.MacdBuy,
                Sma50Above200 = request.Sma50Above200,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Strategies.AddAsync(strategy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Strategy {StrategyId} created for user {UserId}", strategy.Id, userId);

            return _mapper.Map<StrategyResponse>(strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating strategy for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<StrategyResponse>> GetUserStrategiesAsync(Guid userId)
    {
        try
        {
            var strategies = await _unitOfWork.Strategies.GetByUserIdAsync(userId);
            var sortedStrategies = strategies.OrderByDescending(s => s.CreatedAt).ToList();
            return _mapper.Map<List<StrategyResponse>>(sortedStrategies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting strategies for user {UserId}", userId);
            throw;
        }
    }

    public async Task<StrategyResponse> GetStrategyByIdAsync(Guid strategyId, Guid userId)
    {
        try
        {
            var strategy = await _unitOfWork.Strategies.FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId)
                ?? throw new InvalidOperationException("Strategia nie znaleziona");

            return _mapper.Map<StrategyResponse>(strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting strategy {StrategyId} for user {UserId}", strategyId, userId);
            throw;
        }
    }

    public async Task<BaseResponse> UpdateStrategyAsync(Guid strategyId, Guid userId, CreateStrategyRequest request)
    {
        try
        {
            ValidateStrategyRequest(request);

            var strategy = await _unitOfWork.Strategies.FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId)
                ?? throw new InvalidOperationException("Strategia nie znaleziona");

            strategy.Name = request.Name;
            strategy.TargetReturn = request.TargetReturn;
            strategy.InvestmentHorizon = request.InvestmentHorizon;
            strategy.RsiLow = request.RsiLow;
            strategy.RsiHigh = request.RsiHigh;
            strategy.MacdBuy = request.MacdBuy;
            strategy.Sma50Above200 = request.Sma50Above200;

            _unitOfWork.Strategies.Update(strategy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Strategy {StrategyId} updated for user {UserId}", strategyId, userId);

            return new BaseResponse
            {
                Success = true,
                Message = "Strategia zaktualizowana"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating strategy {StrategyId} for user {UserId}", strategyId, userId);
            throw;
        }
    }

    public async Task<BaseResponse> DeleteStrategyAsync(Guid strategyId, Guid userId)
    {
        try
        {
            var strategy = await _unitOfWork.Strategies.FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId)
                ?? throw new InvalidOperationException("Strategia nie znaleziona");

            _unitOfWork.Strategies.Remove(strategy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Strategy {StrategyId} deleted for user {UserId}", strategyId, userId);

            return new BaseResponse
            {
                Success = true,
                Message = "Strategia usunięta"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting strategy {StrategyId} for user {UserId}", strategyId, userId);
            throw;
        }
    }

    public async Task<ToggleStrategyResponse> ToggleStrategyAsync(Guid strategyId, Guid userId)
    {
        try
        {
            var strategy = await _unitOfWork.Strategies.FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId)
                ?? throw new InvalidOperationException("Strategia nie znaleziona");

            // Jeśli aktywujemy strategię, wyłącz wszystkie inne strategie użytkownika
            if (!strategy.IsActive)
            {
                var otherStrategies = await _unitOfWork.Strategies.FindAsync(s => s.UserId == userId && s.IsActive && s.Id != strategyId);
                foreach (var otherStrategy in otherStrategies)
                {
                    otherStrategy.IsActive = false;
                    _unitOfWork.Strategies.Update(otherStrategy);
                    _logger.LogInformation("Deactivated strategy {DeactivatedStrategyId} because user {UserId} activated another strategy {ActivatedStrategyId}", 
                        otherStrategy.Id, userId, strategyId);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            strategy.IsActive = !strategy.IsActive;
            _unitOfWork.Strategies.Update(strategy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Strategy {StrategyId} toggled to {IsActive} for user {UserId}", 
                strategyId, strategy.IsActive, userId);

            // Jeśli strategia jest aktywowana, uruchom analizę
            if (strategy.IsActive)
            {
                _logger.LogInformation("Uruchamiam analizę dla aktywowanej strategii {StrategyId}", strategyId);
                await _strategyExecutionService.ExecuteStrategyAsync(strategyId, userId);

                // Utwórz powiadomienie potwierdzające aktywację
                var activationNotification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    StrategyId = strategyId,
                    Symbol = "SYSTEM",
                    StockName = "System",
                    Message = $"Strategia '{strategy.Name}' została aktywowana i rozpoczęła analizę",
                    Type = NotificationType.Buy,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(activationNotification);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Utworzono powiadomienie aktywacji dla strategii {StrategyId}", strategyId);

                // Wyślij powiadomienie w czasie rzeczywistym
                if (_notificationHubClient != null)
                {
                    try
                    {
                        var notificationDto = _mapper.Map<NotificationResponse>(activationNotification);
                        await _notificationHubClient.SendNotificationAsync(userId, notificationDto);
                    }
                    catch (Exception signalREx)
                    {
                        _logger.LogWarning(signalREx, "Błąd przy wysyłaniu powiadomienia aktywacji przez SignalR");
                    }
                }
            }

            return new ToggleStrategyResponse
            {
                Success = true,
                IsActive = strategy.IsActive,
                Message = strategy.IsActive ? "Strategia aktywowana" : "Strategia dezaktywowana"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling strategy {StrategyId} for user {UserId}", strategyId, userId);
            throw;
        }
    }

    private void ValidateStrategyRequest(CreateStrategyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Nazwa strategii jest wymagana");

        if (request.TargetReturn <= 0)
            throw new InvalidOperationException("Planowana stopa zwrotu musi być większa od zera");

        if (request.InvestmentHorizon <= 0)
            throw new InvalidOperationException("Horyzont inwestycyjny musi być większy od zera");

        if (request.RsiLow < 0 || request.RsiLow > 100)
            throw new InvalidOperationException("RSI niski musi być między 0 a 100");

        if (request.RsiHigh < 0 || request.RsiHigh > 100)
            throw new InvalidOperationException("RSI wysoki musi być między 0 a 100");

        if (request.RsiLow >= request.RsiHigh)
            throw new InvalidOperationException("RSI niski musi być mniejszy niż RSI wysoki");
    }
}
