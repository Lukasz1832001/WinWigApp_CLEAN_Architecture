using WinWigApp.Domain.Entities;
using WinWigApp.Infrastructure.Data;

namespace WinWigApp.Infrastructure.Repositories;

public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(WinWigDbContext context) : base(context)
    {
    }

    public async Task<List<Transaction>> GetByUserIdAsync(Guid userId)
    {
        var transactions = await FindAsync(t => t.UserId == userId);
        return transactions.OrderByDescending(t => t.Timestamp).ToList();
    }
}
