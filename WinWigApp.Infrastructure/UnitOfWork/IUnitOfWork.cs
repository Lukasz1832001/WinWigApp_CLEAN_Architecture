using WinWigApp.Infrastructure.Repositories;

namespace WinWigApp.Infrastructure.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITransactionRepository Transactions { get; }
    IPortfolioRepository Portfolios { get; }
    IDepositRepository Deposits { get; }
    IStrategyRepository Strategies { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
