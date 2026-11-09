
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Moq;
using VehicleStatusSystem.Caching;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Services;
using Xunit;

namespace VehicleStatusSystem.Tests;

public class DashboardCacheConcurrencyTests
{
    [Fact]
    public async Task Invalidation_Should_Remove_Stale_Refill()
    {
        // 1. 模拟数据库最初有 34 辆车
        int vehicleCount = 34;

        // 2. 控制 SQL 查询暂停和恢复的两个信号
        var oldValueRead = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var allowQueryReturn = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // 3. 模拟 Dashboard Repository
        var repository = new Mock<IDashboardRepository>();

        repository
            .Setup(x => x.GetSummaryAsync())
            .Returns(async () =>
            {
                // 查询时截取旧快照
                int snapshot = Volatile.Read(ref vehicleCount);

                if (snapshot == 34)
                {
                    // 通知测试线程：旧值已经读到
                    oldValueRead.TrySetResult();

                    // 故意暂停，不立即返回数据
                    await allowQueryReturn.Task;
                }

                return new DashboardSummaryDto
                {
                    TotalVehicles = snapshot,
                    ActiveVehicles = snapshot,
                    InactiveVehicles = 0,
                    TotalRecords = 43
                };
            });

        // 4. 用线程安全字典模拟缓存
        var storage =
            new ConcurrentDictionary<string, byte[]>();

        var cache = new Mock<IDistributedCache>();

        cache
            .Setup(x => x.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
                storage.TryGetValue(key, out var bytes)
                    ? bytes
                    : null);

        cache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[],
                DistributedCacheEntryOptions, CancellationToken>(
                (key, bytes, options, ct) =>
                {
                    storage[key] = bytes;
                })
            .Returns(Task.CompletedTask);

        cache
            .Setup(x => x.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>(
                (key, ct) =>
                {
                    storage.TryRemove(key, out _);
                })
            .Returns(Task.CompletedTask);

        // 5. 两个 Service 共享同一个 Gate
        var gate = new DashboardCacheGate();


        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["CacheSettings:DashboardEnabled"] = "true"
                })
            .Build();

        var dashboardService = new DashboardService(
            repository.Object,
            cache.Object,
            NullLogger<DashboardService>.Instance,
            gate,
            configuration);


        var invalidator = new DashboardCacheInvalidator(
            cache.Object,
            gate,
            NullLogger<DashboardCacheInvalidator>.Instance);

        try
        {
            // 6. A 开始查询，拿到旧值后暂停
            Task<DashboardSummaryDto> requestA =
                dashboardService.GetSummaryAsync();

            await oldValueRead.Task.WaitAsync(
                TimeSpan.FromSeconds(5));

            // 7. B 模拟数据库提交新值 35
            Volatile.Write(ref vehicleCount, 35);

            // 8. B 请求删除缓存，但应等待 A 的锁
            Task requestB = invalidator.InvalidateAsync();

            Assert.False(requestB.IsCompleted);

            // 9. 恢复 A，允许旧值回填
            allowQueryReturn.TrySetResult();

            DashboardSummaryDto resultA =
                await requestA.WaitAsync(
                    TimeSpan.FromSeconds(5));

            await requestB.WaitAsync(
                TimeSpan.FromSeconds(5));

            // A 取得的是旧数据库快照
            Assert.Equal(34, resultA.TotalVehicles);

            // 10. B 完成后，旧缓存必须被删除
            Assert.False(
                storage.ContainsKey(CacheKeys.DashboardSummary));

            // 11. C 再次查询，应该得到新值
            DashboardSummaryDto resultC =
                await dashboardService.GetSummaryAsync()
                    .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(35, resultC.TotalVehicles);

            // 12. 验证数据库回源了两次
            repository.Verify(
                x => x.GetSummaryAsync(),
                Times.Exactly(2));
        }
        finally
        {
            // 防止断言失败时让暂停的请求一直等待
            allowQueryReturn.TrySetResult();
        }
    }
}
