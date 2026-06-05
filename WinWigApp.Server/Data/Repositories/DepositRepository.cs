using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

public class DepositRepository : GenericRepository<Deposit>, IDepositRepository
{
    public DepositRepository(WinWigDbContext context) : base(context)
    {
    }

    public async Task<List<Deposit>> GetByUserIdAsync(Guid userId)
    {
        var deposits = await FindAsync(d => d.UserId == userId);
        return deposits.OrderByDescending(d => d.Timestamp).ToList();
    }
}
