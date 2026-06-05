using WinWigApp.Server.Data;
using WinWigApp.Server.DTOs;
using WinWigApp.Server.Models;
using AutoMapper;

namespace WinWigApp.Server.Services;

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
            throw new InvalidOperationException("U¿ytkownik z tym emailem ju¿ istnieje");

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Balance = 0m, // Start with zero balance, user must deposit money first
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
            throw new InvalidOperationException("Z³y email lub has³o");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Z³y email lub has³o");

        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            User = _mapper.Map<UserResponse>(user)
        };
    }
}
