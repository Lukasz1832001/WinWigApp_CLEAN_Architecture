namespace WinWigApp.Application.DTOs;

public class RecommendationResult
{
    public string Recommendation { get; set; } = "Czekaj";
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}