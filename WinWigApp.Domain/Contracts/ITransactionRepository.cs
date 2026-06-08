using WinWigApp.Domain.Models;

namespace WinWigApp.Domain.Contracts;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<List<Transaction>> GetByUserIdAsync(Guid userId);
}
