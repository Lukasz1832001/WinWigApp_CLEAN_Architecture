using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WinWigApp.Server.DTOs;
using WinWigApp.Server.Services;

namespace WinWigApp.Server.Controllers;

[ApiController]
[Route("api/strategies")]
[Authorize]
public class StrategiesController : ControllerBase
{
    private readonly IStrategyService _strategyService;
    private readonly ILogger<StrategiesController> _logger;

    public StrategiesController(IStrategyService strategyService, ILogger<StrategiesController> logger)
    {
        _strategyService = strategyService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Nie można pobrać ID użytkownika");
        return userId;
    }

    [HttpPost]
    public async Task<ActionResult<StrategyResponse>> CreateStrategy([FromBody] CreateStrategyRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _strategyService.CreateStrategyAsync(userId, request);
            return CreatedAtAction(nameof(GetStrategy), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Strategy creation validation failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strategy creation error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas tworzenia strategii" });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<StrategyResponse>>> GetStrategies()
    {
        try
        {
            var userId = GetUserId();
            var response = await _strategyService.GetUserStrategiesAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get strategies error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas pobierania strategii" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StrategyResponse>> GetStrategy(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var response = await _strategyService.GetStrategyByIdAsync(id, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Strategy not found");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get strategy error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas pobierania strategii" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseResponse>> UpdateStrategy(Guid id, [FromBody] CreateStrategyRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _strategyService.UpdateStrategyAsync(id, userId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Strategy update failed");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update strategy error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas aktualizacji strategii" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BaseResponse>> DeleteStrategy(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var response = await _strategyService.DeleteStrategyAsync(id, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Strategy deletion failed");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete strategy error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas usuwania strategii" });
        }
    }

    [HttpPut("{id}/toggle")]
    public async Task<ActionResult<ToggleStrategyResponse>> ToggleStrategy(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var response = await _strategyService.ToggleStrategyAsync(id, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Strategy toggle failed");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toggle strategy error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas przełączania strategii" });
        }
    }
}
