using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Services;

public interface INotificationHubClient
{
    Task SendNotificationAsync(Guid userId, NotificationResponse notification);
    Task SendMultipleNotificationsAsync(Guid userId, List<NotificationResponse> notifications);
}
