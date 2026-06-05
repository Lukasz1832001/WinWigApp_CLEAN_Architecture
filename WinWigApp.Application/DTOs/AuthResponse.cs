namespace WinWigApp.Application.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
}
