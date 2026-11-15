using SportsCourtBooking.Infrastructure.Services;

namespace SportsCourtBooking.Web.Services;

public sealed class PendingBookingExpirationWorker : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingBookingExpirationWorker> _logger;

    public PendingBookingExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingBookingExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExpireOverdueBookingsAsync(stoppingToken);

        using var timer = new PeriodicTimer(ScanInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExpireOverdueBookingsAsync(stoppingToken);
        }
    }

    private async Task ExpireOverdueBookingsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var expirationService = scope.ServiceProvider.GetRequiredService<PendingBookingExpirationService>();
        var expiredCount = await expirationService.ExpireOverdueAsync(cancellationToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation("Đã chuyển {Count} booking Pending quá hạn sang Expired.", expiredCount);
        }
    }
}
