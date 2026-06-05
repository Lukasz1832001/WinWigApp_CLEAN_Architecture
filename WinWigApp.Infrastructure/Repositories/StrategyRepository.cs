using WinWigApp.Domain.Entities;
using WinWigApp.Infrastructure.Data;

namespace WinWigApp.Infrastructure.Repositories;

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
