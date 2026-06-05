using WinWigApp.Domain.Entities;

namespace WinWigApp.Infrastructure.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<List<Transaction>> GetByUserIdAsync(Guid userId);
}
