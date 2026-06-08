using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WinWigApp.Application.Services;
using WinWigApp.Server.Middleware;
using WinWigApp.Server.Filters;
using FluentValidation;
using WinWigApp.Application.Validators;
using WinWigApp.Application.Mapping;
using WinWigApp.Infrastructure;
using WinWigApp.Application.Hubs;
using WinWigApp.Domain.Contracts;

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
builder.Services.AddScoped<ITechnicalRecommendationService, TechnicalRecommendationService>();
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
        };
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Controllers
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

var app = builder.Build();

// Auto-create database if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WinWigDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Swagger/OpenAPI temporarily disabled due to Microsoft.OpenApi version conflict.
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();