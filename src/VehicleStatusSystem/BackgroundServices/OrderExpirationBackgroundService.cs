using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.BackgroundServices;

public class OrderExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExpirationBackgroundService> _logger;

    private static readonly TimeSpan ScanInterval =
        TimeSpan.FromMinutes(1);

    private const int BatchSize = 100;

    public OrderExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Order expiration background service started.");

        // 启动后先扫描一次，处理应用停机期间积累的超时订单
        await RunScanSafelyAsync(stoppingToken);

        using var timer = new PeriodicTimer(ScanInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunScanSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // 应用正常停止
        }

        _logger.LogInformation(
            "Order expiration background service stopped.");
    }

    private async Task RunScanSafelyAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            await ProcessExpiredOrdersAsync(stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // 正常关闭时不记录为错误
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Order expiration scan failed.");
        }
    }

    private async Task ProcessExpiredOrdersAsync(
        CancellationToken stoppingToken)
    {
        // HostedService 是 Singleton，
        // Repository 是 Scoped，因此必须创建作用域
        using IServiceScope scope =
            _scopeFactory.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IRentalOrderRepository>();

        List<int> orderIds =
            await repository.GetExpiredOrderIdsAsync(BatchSize);

        _logger.LogInformation(
            "Found {Count} expired order candidates.",
            orderIds.Count);

        foreach (int orderId in orderIds)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                bool expired =
                    await repository.ExpireOrderAsync(orderId);

                if (expired)
                {
                    _logger.LogInformation(
                        "Rental order {OrderId} expired.",
                        orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to expire rental order {OrderId}.",
                    orderId);
            }
        }
    }
}
