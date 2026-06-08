using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WinWigApp.Application.DTOs;
using WinWigApp.Application.Hubs;

namespace WinWigApp.Application.Services;

public class NotificationHubClient : INotificationHubClient
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationHubClient> _logger;

    public NotificationHubClient(IHubContext<NotificationHub> hubContext, ILogger<NotificationHubClient> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Guid userId, NotificationResponse notification)
    {
        try
        {
            var groupName = $"user_{userId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Powiadomienie wysłane do użytkownika {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd przy wysyłaniu powiadomienia do użytkownika {UserId}", userId);
        }
    }

    public async Task SendMultipleNotificationsAsync(Guid userId, List<NotificationResponse> notifications)
    {
        try
        {
            var groupName = $"user_{userId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotifications", notifications);
            _logger.LogInformation("Wysłano {Count} powiadomień do użytkownika {UserId}", notifications.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd przy wysyłaniu powiadomień do użytkownika {UserId}", userId);
        }
    }
}
