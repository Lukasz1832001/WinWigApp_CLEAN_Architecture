using WinWigApp.Domain.Entities;

namespace WinWigApp.Application.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
