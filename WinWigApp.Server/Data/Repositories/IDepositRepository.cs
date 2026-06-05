using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

public interface IDepositRepository : IRepository<Deposit>
{
    Task<List<Deposit>> GetByUserIdAsync(Guid userId);
}
