using WinWigApp.Domain.Entities;
using WinWigApp.Infrastructure.Data;

namespace WinWigApp.Infrastructure.Repositories;

public class PortfolioRepository : GenericRepository<Portfolio>, IPortfolioRepository
{
    public PortfolioRepository(WinWigDbContext context) : base(context)
    {
    }

    public async Task<List<Portfolio>> GetByUserIdAsync(Guid userId)
    {
        var portfolios = await FindAsync(p => p.UserId == userId);
        return portfolios.ToList();
    }
}
