using WinWigApp.Domain.Entities;

namespace WinWigApp.Infrastructure.Repositories;

public interface IStrategyRepository : IRepository<Strategy>
{
    Task<List<Strategy>> GetByUserIdAsync(Guid userId);
}
