using Microsoft.EntityFrameworkCore.Storage;
using WinWigApp.Domain.Contracts;
using WinWigApp.Infrastructure.Contracts;

namespace WinWigApp.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly WinWigDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _userRepository;
    private ITransactionRepository? _transactionRepository;
    private IPortfolioRepository? _portfolioRepository;
    private IDepositRepository? _depositRepository;
    private IStrategyRepository? _strategyRepository;
    private INotificationRepository? _notificationRepository;

    public UnitOfWork(WinWigDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public ITransactionRepository Transactions => _transactionRepository ??= new TransactionRepository(_context);
    public IPortfolioRepository Portfolios => _portfolioRepository ??= new PortfolioRepository(_context);
    public IDepositRepository Deposits => _depositRepository ??= new DepositRepository(_context);
    public IStrategyRepository Strategies => _strategyRepository ??= new StrategyRepository(_context);
    public INotificationRepository Notifications => _notificationRepository ??= new NotificationRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            await SaveChangesAsync();
            await _transaction?.CommitAsync()!;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            await _transaction?.RollbackAsync()!;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}
