using WinWigApp.Infrastructure.Data;
using WinWigApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WinWigApp.Application.Services;

public class SeederService : ISeederService
{
    private readonly WinWigDbContext _context;
    private readonly ILogger<SeederService> _logger;

    public SeederService(WinWigDbContext context, ILogger<SeederService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDefaultStrategiesAsync(Guid userId)
    {
        try
        {
            // Sprawdź czy użytkownik już ma strategie
            var existingStrategies = _context.Strategies.Any(s => s.UserId == userId);
            if (existingStrategies)
            {
                _logger.LogInformation("User {UserId} already has strategies, skipping seed", userId);
                return;
            }

            var strategies = new List<Strategy>
            {
                // Strategia 1: Bezpieczna dla Początkujących
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Bezpieczna dla Początkujących",
                    TargetReturn = 8m,
                    InvestmentHorizon = 45,
                    RsiLow = 35m,
                    RsiHigh = 70m,
                    MacdBuy = true,
                    Sma50Above200 = true,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                },

                // Strategia 2: Zbilansowana
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Zbilansowana",
                    TargetReturn = 12m,
                    InvestmentHorizon = 30,
                    RsiLow = 30m,
                    RsiHigh = 70m,
                    MacdBuy = true,
                    Sma50Above200 = true,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                },

                // Strategia 3: Agresywna - Łap Upadające Noże
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Agresywna - Łap Upadające Noże",
                    TargetReturn = 15m,
                    InvestmentHorizon = 20,
                    RsiLow = 20m,
                    RsiHigh = 75m,
                    MacdBuy = false,
                    Sma50Above200 = false,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Strategies.AddRange(strategies);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded 3 default strategies for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default strategies for user {UserId}", userId);
            throw;
        }
    }
}
