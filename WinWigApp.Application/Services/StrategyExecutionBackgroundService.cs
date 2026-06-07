using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WinWigApp.Application.Services;

namespace WinWigApp.Application.Services;

public class StrategyExecutionBackgroundService : BackgroundService
{
    private readonly ILogger<StrategyExecutionBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _executionInterval = TimeSpan.FromSeconds(30); // Analizuj co 30 sekund

    public StrategyExecutionBackgroundService(ILogger<StrategyExecutionBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StrategyExecutionBackgroundService został uruchomiony");

        // Poczekaj krótko przed pierwszą analizą
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("StrategyExecutionBackgroundService jest zatrzymywany podczas inicjalizacji");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Uruchamiam cykliczną analizę strategii");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var strategyExecutionService = scope.ServiceProvider.GetRequiredService<IStrategyExecutionService>();
                    await strategyExecutionService.ExecuteAllActiveStrategiesAsync();
                }

                _logger.LogInformation("Cykliczna analiza strategii zakończona");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd w cyklicznej analizie strategii");
            }

            try
            {
                await Task.Delay(_executionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("StrategyExecutionBackgroundService jest zatrzymywany");
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StrategyExecutionBackgroundService został zatrzymany");
        await base.StopAsync(cancellationToken);
    }
}
