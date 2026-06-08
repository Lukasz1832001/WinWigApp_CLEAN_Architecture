namespace WinWigApp.Domain.Contracts;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITransactionRepository Transactions { get; }
    IPortfolioRepository Portfolios { get; }
    IDepositRepository Deposits { get; }
    IStrategyRepository Strategies { get; }
    INotificationRepository Notifications { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
