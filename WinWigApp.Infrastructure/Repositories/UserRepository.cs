using WinWigApp.Domain.Contracts;
using WinWigApp.Domain.Models;

namespace WinWigApp.Infrastructure.Contracts;

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
