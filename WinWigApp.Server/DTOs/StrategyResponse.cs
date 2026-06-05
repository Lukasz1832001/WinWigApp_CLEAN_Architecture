namespace WinWigApp.Server.DTOs;

public class StrategyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetReturn { get; set; }
    public int InvestmentHorizon { get; set; }
    public decimal RsiLow { get; set; }
    public decimal RsiHigh { get; set; }
    public bool MacdBuy { get; set; }
    public bool Sma50Above200 { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
