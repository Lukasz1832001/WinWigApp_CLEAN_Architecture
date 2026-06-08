using AutoMapper;
using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Models;
using Microsoft.Extensions.Logging;
using WinWigApp.Domain.Contracts;

namespace WinWigApp.Application.Services;

public interface INotificationService
{
    Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task<List<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId);
    Task<BaseResponse> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<BaseResponse> MarkAllAsReadAsync(Guid userId);
    Task<BaseResponse> DeleteNotificationAsync(Guid notificationId, Guid userId);
    Task CleanupOldNotificationsAsync();
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        try
        {
            var notifications = await _unitOfWork.Notifications.GetUserNotificationsAsync(userId);
            var sortedNotifications = notifications.OrderByDescending(n => n.CreatedAt).ToList();
            return _mapper.Map<List<NotificationResponse>>(sortedNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId)
    {
        try
        {
            var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsAsync(userId);
            var sortedNotifications = notifications.OrderByDescending(n => n.CreatedAt).ToList();
            return _mapper.Map<List<NotificationResponse>>(sortedNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<BaseResponse> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
                ?? throw new InvalidOperationException("Powiadomienie nie znalezione");

            await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);

            return new BaseResponse
            {
                Success = true,
                Message = "Powiadomienie oznaczone jako przeczytane"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    public async Task<BaseResponse> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("All notifications marked as read for user {UserId}", userId);

            return new BaseResponse
            {
                Success = true,
                Message = "Wszystkie powiadomienia oznaczone jako przeczytane"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task<BaseResponse> DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
                ?? throw new InvalidOperationException("Powiadomienie nie znalezione");

            _unitOfWork.Notifications.Remove(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} deleted for user {UserId}", notificationId, userId);

            return new BaseResponse
            {
                Success = true,
                Message = "Powiadomienie usunięte"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task CleanupOldNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Usuwanie starych powiadomień");
            await _unitOfWork.Notifications.DeleteOldNotificationsAsync(daysOld: 30);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Stare powiadomienia zostały usunięte");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
        }
    }
}
