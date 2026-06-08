using WinWigApp.Domain.Models;

namespace WinWigApp.Domain.Contracts;

public interface IStrategyRepository : IRepository<Strategy>
{
    Task<List<Strategy>> GetByUserIdAsync(Guid userId);
}
