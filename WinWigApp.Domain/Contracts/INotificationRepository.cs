using WinWigApp.Domain.Models;

namespace WinWigApp.Domain.Contracts;

public interface INotificationRepository : IRepository<Notification>
{
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId);
    Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId);
    Task<List<Notification>> GetNotificationsByStrategyAsync(Guid strategyId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task DeleteOldNotificationsAsync(int daysOld);
}
