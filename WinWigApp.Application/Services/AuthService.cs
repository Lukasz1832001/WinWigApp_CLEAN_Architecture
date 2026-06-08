using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Models;
using AutoMapper;
using WinWigApp.Domain.Contracts;

namespace WinWigApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ISeederService _seederService;
    private readonly IMapper _mapper;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, ISeederService seederService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _seederService = seederService;
        _mapper = mapper;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Użytkownik z tym emailem już istnieje");

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Balance = 0m,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Seed default strategies for new user
        await _seederService.SeedDefaultStrategiesAsync(user.Id);

        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            User = _mapper.Map<UserResponse>(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            throw new InvalidOperationException("Zły email lub hasło");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Zły email lub hasło");

        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            User = _mapper.Map<UserResponse>(user)
        };
    }
}
