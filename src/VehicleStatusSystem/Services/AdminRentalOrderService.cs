
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class AdminRentalOrderService
    : IAdminRentalOrderService
{
    private readonly IAdminRentalOrderRepository _repository;
    private readonly ILogger<AdminRentalOrderService> _logger;

    public AdminRentalOrderService(
        IAdminRentalOrderRepository repository,
        ILogger<AdminRentalOrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AdminRentalOrderPageDto> GetPagedAsync(
        AdminRentalOrderQueryDto query)
    {
        if (query is null)
        {
            throw new ValidationException(
                "Query parameters are required.");
        }

        // 1. 分页参数校验
        if (query.Page < 1)
        {
            throw new ValidationException(
                "Page must be greater than zero.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            throw new ValidationException(
                "PageSize must be between 1 and 100.");
        }

        // 2. 用户 ID 校验
        if (query.CustomerId.HasValue &&
            query.CustomerId.Value <= 0)
        {
            throw new ValidationException(
                "Invalid customer id.");
        }

        // 3. 状态参数处理
        query.Status = query.Status?.Trim();

        if (query.Status == "")
        {
            query.Status = null;
        }

        if (query.Status is { Length: > 30 })
        {
            throw new ValidationException(
                "Invalid order status length.");
        }

        // 4. 时间范围校验
        if (query.CreatedFrom.HasValue &&
            query.CreatedTo.HasValue &&
            query.CreatedFrom.Value >= query.CreatedTo.Value)
        {
            throw new ValidationException(
                "CreatedFrom must be earlier than CreatedTo.");
        }

        // 约定传入 UTC 时间
        if (query.CreatedFrom.HasValue &&
            query.CreatedFrom.Value.Kind != DateTimeKind.Utc)
        {
            throw new ValidationException(
                "CreatedFrom must be UTC.");
        }

        if (query.CreatedTo.HasValue &&
            query.CreatedTo.Value.Kind != DateTimeKind.Utc)
        {
            throw new ValidationException(
                "CreatedTo must be UTC.");
        }

        _logger.LogInformation(
            "Admin rental order query: Page={Page}, PageSize={PageSize}, Status={Status}",
            query.Page,
            query.PageSize,
            query.Status);

        // 5. 调用 Repository
        return await _repository.GetPagedAsync(query);
    }
}
