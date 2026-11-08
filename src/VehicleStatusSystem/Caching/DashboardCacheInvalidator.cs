using Microsoft.Extensions.Caching.Distributed;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Caching;

public sealed class DashboardCacheInvalidator
    : IDashboardCacheInvalidator
{
    private readonly IDistributedCache _cache;
    private readonly DashboardCacheGate _cacheGate;
    private readonly ILogger<DashboardCacheInvalidator> _logger;

    public DashboardCacheInvalidator(
        IDistributedCache cache,
        DashboardCacheGate cacheGate,
        ILogger<DashboardCacheInvalidator> logger)
    {
        _cache = cache;
        _cacheGate = cacheGate;
        _logger = logger;
    }

    public async Task InvalidateAsync()
    {
        // 与 Dashboard 回源使用同一把锁
        await _cacheGate.Semaphore.WaitAsync();

        try
        {
            // 数据库提交成功后删除缓存
            await _cache.RemoveAsync(
                CacheKeys.DashboardSummary);

            _logger.LogInformation(
                "Dashboard cache invalidated under shared lock. Key={CacheKey}",
                CacheKeys.DashboardSummary);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Dashboard cache invalidation failed. Key={CacheKey}",
                CacheKeys.DashboardSummary);
        }
        finally
        {
            // 无论 Redis 删除是否成功都必须释放锁
            _cacheGate.Semaphore.Release();
        }
    }
}
