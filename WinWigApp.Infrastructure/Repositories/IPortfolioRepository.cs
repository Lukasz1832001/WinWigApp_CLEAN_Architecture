using WinWigApp.Domain.Entities;

namespace WinWigApp.Infrastructure.Repositories;

public interface IPortfolioRepository : IRepository<Portfolio>
{
    Task<List<Portfolio>> GetByUserIdAsync(Guid userId);
}
