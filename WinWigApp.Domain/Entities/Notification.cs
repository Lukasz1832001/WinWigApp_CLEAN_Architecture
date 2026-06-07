namespace WinWigApp.Domain.Entities;

public enum NotificationType
{
    Buy,
    Sell
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    // Foreign keys and navigation properties
    public User User { get; set; } = null!;
    public Strategy Strategy { get; set; } = null!;
}
