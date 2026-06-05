using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<List<Transaction>> GetByUserIdAsync(Guid userId);
}
