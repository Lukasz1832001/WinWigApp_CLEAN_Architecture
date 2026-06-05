using WinWigApp.Server.Models;

namespace WinWigApp.Server.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
