
namespace VehicleStatusSystem.Caching;

public sealed class DashboardCacheGate
{
    // 同一时刻只允许一个操作持有锁
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
