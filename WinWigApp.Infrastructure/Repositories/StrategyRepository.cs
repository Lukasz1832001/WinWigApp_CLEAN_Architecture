using WinWigApp.Domain.Contracts;
using WinWigApp.Domain.Models;

namespace WinWigApp.Infrastructure.Contracts;

public class StrategyRepository : GenericRepository<Strategy>, IStrategyRepository
{
    public StrategyRepository(WinWigDbContext context) : base(context)
    {
    }

    public async Task<List<Strategy>> GetByUserIdAsync(Guid userId)
    {
        var strategies = await FindAsync(s => s.UserId == userId);
        return strategies.ToList();
    }
}
