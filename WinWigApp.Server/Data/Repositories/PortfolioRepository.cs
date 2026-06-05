using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

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
