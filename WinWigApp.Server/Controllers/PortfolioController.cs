using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WinWigApp.Server.Data;
using WinWigApp.Server.DTOs;
using System.Linq;
using AutoMapper;

namespace WinWigApp.Server.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly WinWigDbContext _context;
    private readonly ILogger<PortfolioController> _logger;
    private readonly IMapper _mapper;

    public PortfolioController(WinWigDbContext context, ILogger<PortfolioController> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Nie można pobrać ID użytkownika");
        return userId;
    }

    [HttpGet]
    public IActionResult GetPortfolio()
    {
        try
        {
            var userId = GetUserId();

            // Pobranie pozycji portfela użytkownika
            var portfolioItems = _context.Portfolios
                .Where(p => p.UserId == userId)
                .ToList();

            // Mapowanie na DTO
            var items = _mapper.Map<List<PortfolioItemResponse>>(portfolioItems);

            // Obliczanie wartości portfela
            decimal totalValue = 0;
            decimal totalInvested = 0;

            foreach (var item in items)
            {
                totalInvested += item.AvgPrice * item.Quantity;
            }

            // Note: totalValue cannot be calculated without current prices
            // Client should fetch current prices from /api/stocks and calculate based on Portfolio data
            var totalProfit = totalValue - totalInvested;
            var totalProfitPercent = totalInvested > 0 ? (totalProfit / totalInvested) * 100 : 0;

            var response = new PortfolioResponse
            {
                Items = items,
                TotalValue = totalValue,
                TotalInvested = totalInvested,
                TotalProfit = totalProfit,
                TotalProfitPercent = totalProfitPercent
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized portfolio access");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas pobierania portfela" });
        }
    }

    [HttpPut("{symbol}/stoploss")]
    public async Task<IActionResult> UpdateStopLoss([FromRoute] string symbol, [FromBody] UpdateStopLossRequest request)
    {
        try
        {
            var userId = GetUserId();

            // Validate input
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest(new { message = "Symbol jest wymagany" });

            _logger.LogInformation("Updating stop loss for symbol: {Symbol}, userId: {UserId}, newStopLoss: {StopLoss}", symbol, userId, request.StopLoss);

            // Find portfolio position
            var portfolio = _context.Portfolios
                .FirstOrDefault(p => p.UserId == userId && p.Symbol.ToUpper() == symbol.ToUpper());

            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio position not found for symbol: {Symbol}, userId: {UserId}", symbol, userId);
                return NotFound(new { message = "Pozycja nie znaleziona w portfelu" });
            }

            _logger.LogInformation("Found portfolio position: {PortfolioId}, current stopLoss: {CurrentStopLoss}", portfolio.Id, portfolio.StopLoss);

            // Update stop loss
            portfolio.StopLoss = request.StopLoss;

            // Explicitly mark entity as modified
            _context.Portfolios.Update(portfolio);

            var changesCount = await _context.SaveChangesAsync();
            _logger.LogInformation("SaveChangesAsync returned {ChangesCount} for stop loss update", changesCount);

            if (changesCount == 0)
            {
                _logger.LogWarning("No changes were saved to database for symbol: {Symbol}", symbol);
            }

            _logger.LogInformation("Stop loss updated successfully for symbol: {Symbol}, newStopLoss: {NewStopLoss}", symbol, portfolio.StopLoss);

            return Ok(new { success = true, message = "Stop loss zaktualizowany" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized stop loss update");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stop loss for {Symbol}", symbol);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas aktualizacji stop loss" });
        }
    }

    [HttpDelete("{symbol}/stoploss")]
    public async Task<IActionResult> RemoveStopLoss([FromRoute] string symbol)
    {
        try
        {
            var userId = GetUserId();

            // Validate input
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest(new { message = "Symbol jest wymagany" });

            _logger.LogInformation("Removing stop loss for symbol: {Symbol}, userId: {UserId}", symbol, userId);

            // Find portfolio position
            var portfolio = _context.Portfolios
                .FirstOrDefault(p => p.UserId == userId && p.Symbol.ToUpper() == symbol.ToUpper());

            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio position not found for symbol: {Symbol}, userId: {UserId}", symbol, userId);
                return NotFound(new { message = "Pozycja nie znaleziona w portfelu" });
            }

            _logger.LogInformation("Found portfolio position: {PortfolioId}, current stopLoss: {CurrentStopLoss}", portfolio.Id, portfolio.StopLoss);

            // Remove stop loss
            portfolio.StopLoss = null;

            // Explicitly mark entity as modified
            _context.Portfolios.Update(portfolio);

            var changesCount = await _context.SaveChangesAsync();
            _logger.LogInformation("SaveChangesAsync returned {ChangesCount} for stop loss removal", changesCount);

            if (changesCount == 0)
            {
                _logger.LogWarning("No changes were saved to database for symbol: {Symbol}", symbol);
            }

            _logger.LogInformation("Stop loss removed successfully for symbol: {Symbol}", symbol);

            return Ok(new { success = true, message = "Stop loss usunięty" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized stop loss removal");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stop loss for {Symbol}", symbol);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas usuwania stop loss" });
        }
    }
}
