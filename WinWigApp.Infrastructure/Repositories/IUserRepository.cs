using WinWigApp.Domain.Entities;

namespace WinWigApp.Infrastructure.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
