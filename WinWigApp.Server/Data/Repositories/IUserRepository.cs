using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
