using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

public interface IPortfolioRepository : IRepository<Portfolio>
{
    Task<List<Portfolio>> GetByUserIdAsync(Guid userId);
}
