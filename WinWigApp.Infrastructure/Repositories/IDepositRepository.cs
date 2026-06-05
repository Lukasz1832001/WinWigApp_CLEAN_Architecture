using WinWigApp.Domain.Entities;

namespace WinWigApp.Infrastructure.Repositories;

public interface IDepositRepository : IRepository<Deposit>
{
    Task<List<Deposit>> GetByUserIdAsync(Guid userId);
}
