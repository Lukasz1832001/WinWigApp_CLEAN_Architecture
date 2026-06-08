using WinWigApp.Domain.Contracts;
using WinWigApp.Domain.Models;

namespace WinWigApp.Infrastructure.Contracts;

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
