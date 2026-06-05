using WinWigApp.Domain.Entities;
using WinWigApp.Infrastructure.Data;

namespace WinWigApp.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(WinWigDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await FirstOrDefaultAsync(u => u.Email == email);
    }
}
