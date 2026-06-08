using WinWigApp.Domain.Models;

namespace WinWigApp.Domain.Contracts;

public interface IPortfolioRepository : IRepository<Portfolio>
{
    Task<List<Portfolio>> GetByUserIdAsync(Guid userId);
}
