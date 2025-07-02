using Timoto.DAL;

public class CarCleanupService : BackgroundService
{
    private readonly IServiceProvider _provider;

    public CarCleanupService(IServiceProvider provider)
    {
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cutoff = DateTime.UtcNow.AddHours(4).AddMonths(-2);

                var oldDeactivatedCars = db.Cars
                    .Where(c => c.IsActive == false && c.DeactivatedAt != null && c.DeactivatedAt <= cutoff);

                db.Cars.RemoveRange(oldDeactivatedCars);
                await db.SaveChangesAsync();
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
