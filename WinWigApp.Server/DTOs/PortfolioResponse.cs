namespace WinWigApp.Server.DTOs;

public class PortfolioItemResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal? StopLoss { get; set; }
}

public class PortfolioResponse
{
    public List<PortfolioItemResponse> Items { get; set; } = new();
    public decimal TotalValue { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalProfitPercent { get; set; }
}
