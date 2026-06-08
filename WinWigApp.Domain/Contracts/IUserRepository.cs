using WinWigApp.Domain.Models;

namespace WinWigApp.Domain.Contracts;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
