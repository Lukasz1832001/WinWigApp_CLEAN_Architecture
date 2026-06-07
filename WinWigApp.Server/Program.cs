using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WinWigApp.Infrastructure.Data;
using WinWigApp.Infrastructure.UnitOfWork;
using WinWigApp.Application.Services;
using WinWigApp.Server.Middleware;
using WinWigApp.Server.Filters;
using WinWigApp.Server.Hubs;
using WinWigApp.Server.Services;
using FluentValidation;
using WinWigApp.Application.Validators;
using WinWigApp.Application.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=WinWigApp.db";
builder.Services.AddDbContext<WinWigDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISeederService, SeederService>();
builder.Services.AddHttpClient<IYahooFinanceClient, YahooFinanceClient>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; WinWigApp/1.0)");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategyExecutionService, StrategyExecutionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationHubClient, NotificationHubClient>();
builder.Services.AddHostedService<StrategyExecutionBackgroundService>();

// Register SignalR
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    jwtSecret = "your-secret-key-change-this-in-production-12345678901234567890";
}

var key = System.Text.Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        // Dla SignalR - obsługuj token z query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Connection"] == "Upgrade"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins(
            "http://localhost:5262",
            "http://localhost:5173",
            "http://localhost:3000",
            "https://localhost:7054",
            "https://localhost:3000"
        )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Wymagane dla WebSocket
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WinWigDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.MapStaticAssets();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapFallbackToFile("/index.html");

app.Run();
