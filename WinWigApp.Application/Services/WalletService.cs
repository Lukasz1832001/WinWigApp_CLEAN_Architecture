using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Models;
using AutoMapper;
using WinWigApp.Domain.Contracts;

namespace WinWigApp.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WalletService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<DepositResponse> DepositAsync(Guid userId, DepositRequest request)
    {
        // Validate amount
        if (request.Amount <= 0)
            throw new InvalidOperationException("Kwota musi być większa niż 0");

        if (string.IsNullOrWhiteSpace(request.Method))
            throw new InvalidOperationException("Metoda płatności jest wymagana");

        // Get user
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("Użytkownik nie znaleziony");

        // Update balance
        user.Balance += request.Amount;

        // Create deposit record
        var deposit = new Deposit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = request.Amount,
            Method = request.Method,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.Deposits.AddAsync(deposit);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<DepositResponse>(deposit);
        response.NewBalance = user.Balance;
        return response;
    }

    public async Task<List<DepositsResponse>> GetDepositsAsync(Guid userId)
    {
        var deposits = await _unitOfWork.Deposits.FindAsync(d => d.UserId == userId);
        var sortedDeposits = deposits.OrderByDescending(d => d.Timestamp).ToList();
        return _mapper.Map<List<DepositsResponse>>(sortedDeposits);
    }

    public async Task<BalanceResponse> GetBalanceAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("Użytkownik nie znaleziony");

        return new BalanceResponse
        {
            Balance = user.Balance
        };
    }
}
