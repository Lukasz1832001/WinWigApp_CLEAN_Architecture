using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Entities;
using AutoMapper;
using WinWigApp.Infrastructure.Data;

namespace WinWigApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly WinWigDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ISeederService _seederService;
    private readonly IMapper _mapper;

    public AuthService(WinWigDbContext context, ITokenService tokenService, ISeederService seederService, IMapper mapper)
    {
        _context = context;
        _tokenService = tokenService;
        _seederService = seederService;
        _mapper = mapper;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = _context.Users.FirstOrDefault(u => u.Email == request.Email);
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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

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
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
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
