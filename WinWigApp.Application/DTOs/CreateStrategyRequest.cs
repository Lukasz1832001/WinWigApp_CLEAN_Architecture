namespace WinWigApp.Application.DTOs;

public class CreateStrategyRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetReturn { get; set; }
    public int InvestmentHorizon { get; set; }
    public decimal RsiLow { get; set; }
    public decimal RsiHigh { get; set; }
    public bool MacdBuy { get; set; }
    public bool Sma50Above200 { get; set; }
}
