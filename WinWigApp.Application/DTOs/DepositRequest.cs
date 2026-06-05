namespace WinWigApp.Application.DTOs;

public class DepositRequest
{
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
}
