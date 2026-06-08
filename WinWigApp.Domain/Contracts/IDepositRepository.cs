using WinWigApp.Domain.Models;
namespace WinWigApp.Domain.Contracts;

public interface IDepositRepository : IRepository<Deposit>
{
    Task<List<Deposit>> GetByUserIdAsync(Guid userId);
}
