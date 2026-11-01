
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class VehicleAvailabilityService
    : IVehicleAvailabilityService
{
    private readonly IVehicleAvailabilityRepository _repository;

    public VehicleAvailabilityService(
        IVehicleAvailabilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<AvailabilityPageDto> GetAvailableAsync(
        AvailabilityQueryDto query)
    {
        // 1. 检查必填时间
        if (!query.StartAt.HasValue ||
            !query.EndAt.HasValue)
        {
            throw new ValidationException(
                "StartAt and EndAt are required.");
        }

        // 2. 统一转换为 UTC
        DateTime startAtUtc =
            query.StartAt.Value.UtcDateTime;

        DateTime endAtUtc =
            query.EndAt.Value.UtcDateTime;

        // 3. 检查租期是否合法
        if (startAtUtc >= endAtUtc)
        {
            throw new ValidationException(
                "EndAt must be later than StartAt.");
        }

        // 4. 不允许查询过去的预约时间
        if (startAtUtc < DateTime.UtcNow)
        {
            throw new ValidationException(
                "StartAt cannot be in the past.");
        }

        // 5. 检查车型 ID
        if (query.ModelId.HasValue &&
            query.ModelId.Value <= 0)
        {
            throw new ValidationException(
                "ModelId must be positive.");
        }

        // 6. 检查分页
        if (query.Page < 1)
        {
            throw new ValidationException(
                "Page must be greater than zero.");
        }

        if (query.PageSize < 1 ||
            query.PageSize > 100)
        {
            throw new ValidationException(
                "PageSize must be between 1 and 100.");
        }

        // 7. 防止分页偏移量溢出
        long offset =
            ((long)query.Page - 1) * query.PageSize;

        if (offset > int.MaxValue)
        {
            throw new ValidationException(
                "Pagination offset is too large.");
        }

        // 8. 调用 Repository
        return await _repository.GetAvailableAsync(
            startAtUtc,
            endAtUtc,
            query.ModelId,
            query.Page,
            query.PageSize);
    }
}
