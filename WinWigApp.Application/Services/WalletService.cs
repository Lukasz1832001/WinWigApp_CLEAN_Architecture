using WinWigApp.Infrastructure.Data;
using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Entities;
using AutoMapper;

namespace WinWigApp.Application.Services;

public class WalletService : IWalletService
{
    private readonly WinWigDbContext _context;
    private readonly IMapper _mapper;

    public WalletService(WinWigDbContext context, IMapper mapper)
    {
        _context = context;
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
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
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

        _context.Deposits.Add(deposit);
        await _context.SaveChangesAsync();

        var response = _mapper.Map<DepositResponse>(deposit);
        response.NewBalance = user.Balance;
        return response;
    }

    public async Task<List<DepositsResponse>> GetDepositsAsync(Guid userId)
    {
        var deposits = _context.Deposits
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.Timestamp)
            .ToList();

        return _mapper.Map<List<DepositsResponse>>(deposits);
    }

    public async Task<BalanceResponse> GetBalanceAsync(Guid userId)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("Użytkownik nie znaleziony");

        return new BalanceResponse
        {
            Balance = user.Balance
        };
    }
}
