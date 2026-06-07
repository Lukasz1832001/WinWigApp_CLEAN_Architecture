namespace WinWigApp.Application.DTOs;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Buy" or "Sell"
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
