using WinWigApp.Domain.Entities;
using WinWigApp.Infrastructure.Data;

namespace WinWigApp.Infrastructure.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(WinWigDbContext context) : base(context)
    {
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId)
    {
        return (await FindAsync(n => n.UserId == userId)).ToList();
    }

    public async Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId)
    {
        return (await FindAsync(n => n.UserId == userId && !n.IsRead)).ToList();
    }

    public async Task<List<Notification>> GetNotificationsByStrategyAsync(Guid strategyId)
    {
        return (await FindAsync(n => n.StrategyId == strategyId)).ToList();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await FirstOrDefaultAsync(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            Update(notification);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await GetUserNotificationsAsync(userId);
        foreach (var notification in notifications.Where(n => !n.IsRead))
        {
            notification.IsRead = true;
            Update(notification);
        }
    }

    public async Task DeleteOldNotificationsAsync(int daysOld)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        var oldNotifications = (await FindAsync(n => n.CreatedAt < cutoffDate)).ToList();
        foreach (var notification in oldNotifications)
        {
            Remove(notification);
        }
    }
}
