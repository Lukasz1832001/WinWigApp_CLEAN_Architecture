using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WinWigApp.Application.DTOs;
using WinWigApp.Application.Services;

namespace WinWigApp.Server.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Nie można pobrać ID użytkownika");
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> GetNotifications()
    {
        try
        {
            var userId = GetUserId();
            var response = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get notifications error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas pobierania powiadomień" });
        }
    }

    [HttpGet("unread")]
    public async Task<ActionResult<List<NotificationResponse>>> GetUnreadNotifications()
    {
        try
        {
            var userId = GetUserId();
            var response = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get unread notifications error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas pobierania nieprzeczytanych powiadomień" });
        }
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<BaseResponse>> MarkAsRead(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var response = await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Notification not found");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mark as read error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas zaznaczania powiadomienia" });
        }
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<BaseResponse>> MarkAllAsRead()
    {
        try
        {
            var userId = GetUserId();
            var response = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mark all as read error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas zaznaczania powiadomień" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BaseResponse>> DeleteNotification(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var response = await _notificationService.DeleteNotificationAsync(id, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Notification deletion failed");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete notification error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Błąd serwera podczas usuwania powiadomienia" });
        }
    }
}
