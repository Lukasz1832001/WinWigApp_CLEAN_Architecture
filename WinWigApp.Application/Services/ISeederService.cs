namespace WinWigApp.Application.Services;

public interface ISeederService
{
    Task SeedDefaultStrategiesAsync(Guid userId);
}
