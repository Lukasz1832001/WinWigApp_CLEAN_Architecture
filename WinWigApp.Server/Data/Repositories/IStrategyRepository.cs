using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

public interface IStrategyRepository : IRepository<Strategy>
{
    Task<List<Strategy>> GetByUserIdAsync(Guid userId);
}
