using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Services;

public interface ITechnicalRecommendationService
{
    RecommendationResult CalculateRecommendation(
        StockResponse stock,
        TechnicalIndicatorsResponse indicators
    );
}