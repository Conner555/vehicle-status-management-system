
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleStatusSystem.Caching;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class DashboardService : IDashboardService

{

    private readonly bool _cacheEnabled;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DashboardService> _logger;

    private const string CacheKey = CacheKeys.DashboardSummary;

    private static readonly TimeSpan CacheTtl =
        TimeSpan.FromSeconds(30);

    // 同进程的 Dashboard 请求共享一把锁
    private readonly DashboardCacheGate _cacheGate;


    public DashboardService(
        IDashboardRepository dashboardRepository,
        IDistributedCache cache,
        ILogger<DashboardService> logger,
        DashboardCacheGate cacheGate,
        IConfiguration configuration)
    {
        _dashboardRepository = dashboardRepository;
        _cache = cache;
        _logger = logger;
        _cacheGate = cacheGate;

        _cacheEnabled = configuration.GetValue<bool>(
            "CacheSettings:DashboardEnabled",
            true);
    }


    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {

        if(!_cacheEnabled)
        {
            return await _dashboardRepository.GetSummaryAsync();
        }
        
        // 第一次检查：无需获取锁
        var cached = await TryGetCachedAsync();

        if (cached is not null)
        {
            _logger.LogInformation("Dashboard Cache HIT.");
            
            return cached;
        }

        // 缓存未命中，等待进入临界区
        await _cacheGate.Semaphore.WaitAsync();

        try
        {
            // 第二次检查：必须保留
            cached = await TryGetCachedAsync();

            if (cached is not null)
            {
                _logger.LogInformation(
                    "Dashboard Cache HIT after lock.");

                return cached;
            }

            _logger.LogInformation(
                "Dashboard Cache MISS. Querying SQL Server.");

            // 只有获得锁且第二次仍 MISS 的请求才回源
            var result =
                await _dashboardRepository.GetSummaryAsync();

            // 数据库成功返回后尝试写入缓存
            await TrySetCachedAsync(result);

            return result;
        }
        finally
        {
            // 无论成功还是异常，都必须释放锁
            _cacheGate.Semaphore.Release();
        }
    }

    private async Task<DashboardSummaryDto?> TryGetCachedAsync()
    {
        try
        {
            string? json =
                await _cache.GetStringAsync(CacheKey);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<DashboardSummaryDto>(
                json);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Dashboard Redis read failed. Key={CacheKey}",
                CacheKey);

            return null;
        }
    }

    private async Task TrySetCachedAsync(
        DashboardSummaryDto result)
    {
        try
        {
            string json = JsonSerializer.Serialize(result);

            await _cache.SetStringAsync(
                CacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                });

            _logger.LogInformation(
                "Dashboard cache saved. TTL={Seconds}s",
                CacheTtl.TotalSeconds);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Dashboard Redis write failed. Key={CacheKey}",
                CacheKey);
        }
    }
}
