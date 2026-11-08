using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Exceptions;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;
using Microsoft.Extensions.Caching.Distributed;
using VehicleStatusSystem.Caching;

namespace VehicleStatusSystem.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleStatusRepository _statusRepository;
    private readonly ILogger<VehicleService> _logger;

    private readonly IDashboardCacheInvalidator _cacheInvalidator;

    private readonly IDistributedCache _cache;

    private readonly VehicleCacheGate _vehicleCacheGate;


    //解决缓存雪崩，设置随机TTL
    int ttlSeconds = Random.Shared.Next(15, 31);


    

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IVehicleStatusRepository statusRepository,
        ILogger<VehicleService> logger,
        IDashboardCacheInvalidator cacheInvalidator,
        IDistributedCache cache,
        VehicleCacheGate vehicleCacheGate
        )
    {
        _vehicleRepository = vehicleRepository;
        _statusRepository = statusRepository;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
        _cache = cache;
        _vehicleCacheGate = vehicleCacheGate;
    }

    public async Task<List<VehicleDto>> GetAllAsync(string? keyword = null, string? status = null)
    {
        List<Vehicle> vehicles =
            await _vehicleRepository.GetAllAsync(
                keyword,
                status
                );

        return vehicles
            .Select(ToDto)
            .ToList();
    }


    public async Task<PagedResultDto<VehicleDto>> GetPagedAsync(
        string? keyword,
        string? status,
        int page,
        int pageSize)
    {
        // 1. 校验分页参数
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page must be greater than or equal to 1."
            );
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "PageSize must be between 1 and 100."
            );
        }

        // 2. 检查 OFFSET 是否超出 int 范围
        if (((long)page - 1) * pageSize > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page offset is too large."
            );
        }

        // 3. 调用 Repository
        var (vehicles, totalCount) =
            await _vehicleRepository.GetPagedAsync(
                keyword,
                status,
                page,
                pageSize
            );

        // 4. 转换为 VehicleDto
        List<VehicleDto> items = vehicles
            .Select(ToDto)
            .ToList();

        // 5. 计算总页数
        int totalPages =
            (int)Math.Ceiling(
                (double)totalCount / pageSize
            );

        // 6. 返回分页结果
        return new PagedResultDto<VehicleDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }




    public async Task<VehicleDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        string cacheKey = CacheKeys.VehicleNotFound(id);

        // 1. 第一次 Redis 检查
        if (await IsNegativeCacheHitAsync(cacheKey))
            return null;

        // 2. 获取这个 VehicleId 对应的锁
        SemaphoreSlim gate = _vehicleCacheGate.GetLock(id);
        await gate.WaitAsync();

        try
        {
            // 3. 锁内第二次检查
            if (await IsNegativeCacheHitAsync(cacheKey))
            {
                _logger.LogInformation(
                    "Vehicle Negative Cache HIT after lock. Id={Id}",
                    id);

                return null;
            }

            // 4. 二次 MISS，查询数据库
            _logger.LogInformation(
                "Vehicle Negative Cache MISS. DB QUERY Id={Id}",
                id);

            Vehicle? vehicle =
                await _vehicleRepository.GetByIdAsync(id);

            if (vehicle is not null)
                return ToDto(vehicle);

            // 5. 不存在，写入短期负向缓存
            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    "NOT_FOUND",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromSeconds(ttlSeconds)
                    });
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Vehicle negative cache write failed.");
            }

            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<bool> IsNegativeCacheHitAsync(
        string cacheKey)
    {
        try
        {
            string? value =
                await _cache.GetStringAsync(cacheKey);

            return value == "NOT_FOUND";
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Vehicle negative cache read failed.");

            return false;
        }
    }



    public async Task<VehicleDto> CreateAsync(
        CreateVehicleDto dto)
    {
        bool exists =
            await _vehicleRepository
                .PlateNumberExistsAsync(dto.PlateNumber);

        if (exists)
        {
            _logger.LogWarning(
                "Vehicle creation rejected because the plate number already exists.");

            throw new BusinessRuleException(
                $"Plate number {dto.PlateNumber} already exists.");
        }

        Vehicle vehicle = new()
        {
            PlateNumber = dto.PlateNumber,
            Model = dto.Model,
            Status = dto.Status
        };

        Vehicle created =
            await _vehicleRepository.AddAsync(vehicle);

        //新建车辆修改SQL数据库内容之后删除Redis缓存
        await _cacheInvalidator.InvalidateAsync();

        _logger.LogInformation(
            "Vehicle {VehicleId} created successfully.",
            created.Id);

        return ToDto(created);
    }

    public async Task<VehicleDto?> UpdateAsync(
        int id,
        UpdateVehicleDto dto)
    {
        Vehicle? existing =
            await _vehicleRepository.GetByIdAsync(id);

        if (existing is null)
        {
            return null;
        }

        bool plateExists =
            await _vehicleRepository
                .PlateNumberExistsAsync(
                    dto.PlateNumber,
                    id);

        if (plateExists)
        {
            throw new BusinessRuleException(
                $"Plate number {dto.PlateNumber} already exists.");
        }

        existing.PlateNumber = dto.PlateNumber;
        existing.Model = dto.Model;
        existing.Status = dto.Status;

        bool update = await _vehicleRepository.UpdateAsync(existing);

        if(!update)
        {
            return null;
        }

        //SQL成功更新才删除缓存
        await _cacheInvalidator.InvalidateAsync();


        _logger.LogInformation(
            "Vehicle {VehicleId} update successfully!",
            existing.Id
        );

        return ToDto(existing);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        Vehicle? vehicle =
            await _vehicleRepository.GetByIdAsync(id);

        if (vehicle is null)
        {
            return false;
        }

        bool hasRecords =
            await _statusRepository.HasRecordsAsync(id);

        if (hasRecords)
        {
            throw new BusinessRuleException(
                "The vehicle has status records and cannot be deleted.");
        }

        bool deleted =
            await _vehicleRepository.DeleteAsync(id);

        if (deleted)
        {
            //成功删除，检查完数据库才失效缓存
            await _cacheInvalidator.InvalidateAsync();

            _logger.LogInformation(
                "Vehicle {VehicleId} deleted successfully.",
                id);
        }

        return deleted;
    }

    private static VehicleDto ToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Model = vehicle.Model,
            Status = vehicle.Status,
            CreateTime = vehicle.CreateTime
        };
    }
}