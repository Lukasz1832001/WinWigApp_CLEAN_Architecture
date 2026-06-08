using WinWigApp.Domain.Models;

namespace WinWigApp.Application.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
