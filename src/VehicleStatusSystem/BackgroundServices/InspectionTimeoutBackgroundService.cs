
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.BackgroundServices;

public sealed class InspectionTimeoutBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InspectionTimeoutBackgroundService> _logger;

    private static readonly TimeSpan ScanInterval =
        TimeSpan.FromMinutes(1);

    public InspectionTimeoutBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InspectionTimeoutBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Inspection timeout monitor started.");

        await ScanSafelyAsync(stoppingToken);

        using var timer = new PeriodicTimer(ScanInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ScanSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // 正常停止
        }

        _logger.LogInformation(
            "Inspection timeout monitor stopped.");
    }




    private async Task ScanSafelyAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            // 1. 获取提醒 Repository
            var reminderRepository = scope.ServiceProvider
                .GetRequiredService<IInspectionReminderRepository>();

            // 2. 同步提醒事件
            var reminder = await reminderRepository.SynchronizeAsync(
                cancellationToken);

            _logger.LogInformation(
                "Inspection reminder sync: Created={Created}, Resolved={Resolved}, Escalated={Escalated}",
                reminder.CreatedCount,
                reminder.ResolvedCount,
                reminder.EscalatedCount);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Inspection reminder synchronization failed.");
        }

        // 提醒同步失败时，仍尝试执行超时扫描
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var timeoutRepository = scope.ServiceProvider
                .GetRequiredService<IInspectionTimeoutRepository>();

            var timeout = await timeoutRepository.SynchronizeEventsAsync(
                100,
                cancellationToken);

            _logger.LogInformation(
                "Inspection timeout sync: Created={Created}, Resolved={Resolved}",
                timeout.CreatedCount,
                timeout.ResolvedCount);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Inspection timeout synchronization failed.");
        }
    }

}
