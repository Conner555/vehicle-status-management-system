using VehicleStatusSystem.Exceptions;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Features;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;
using VehicleStatusSystem.Repositories;

namespace VehicleStatusSystem.Services;

public class RentalOrderService : IRentalOrderService
{
    private readonly IRentalOrderRepository _repository;
    private readonly ILogger<RentalOrderService> _logger;

    // 演示价格：正式版本将从数据库读取
    private const decimal DemoDailyRate = 200m;

    public RentalOrderService(
        IRentalOrderRepository repository,
        ILogger<RentalOrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> CreateAsync(
        int customerId,
        CreateRentalOrderDto dto)
    {
        if (customerId <= 0)
            throw new ValidationException(
                "Invalid customer id.");

        if (dto.Items is null ||
            dto.Items.Count == 0 ||
            dto.Items.Count > 5)
        {
            throw new ValidationException(
                "An order must contain 1 to 5 vehicles.");
        }

        var lines = new List<RentalOrderLine>();
        var vehicleIds = new HashSet<int>();

        foreach (var item in dto.Items)
        {
            if (item is null || item.VehicleId <= 0)
                throw new ValidationException(
                    "Invalid vehicle.");

            if (!vehicleIds.Add(item.VehicleId))
                throw new ValidationException(
                    "Duplicate vehicle in one order.");

            if (item.StartAt is null ||
                item.EndAt is null)
            {
                throw new ValidationException(
                    "Rental start and end are required.");
            }

            DateTime start =
                item.StartAt.Value.UtcDateTime;

            DateTime end =
                item.EndAt.Value.UtcDateTime;

            if (end <= start)
                throw new ValidationException(
                    "EndAt must be later than StartAt.");

            if (start <= DateTime.UtcNow)
                throw new ValidationException(
                    "StartAt must be in the future.");

            // 以每24小时为一天，不足一天向上取整
            int days = checked(
                (int)Math.Ceiling(
                    (end - start).TotalDays));

            lines.Add(new RentalOrderLine(
                item.VehicleId,
                start,
                end,
                days
            ));
        }

        int orderId = await _repository.CreateAsync(
            customerId,
            lines);

        _logger.LogInformation(
            "Rental order {OrderId} created with {Count} vehicles.",
            orderId,
            lines.Count);

        return orderId;
    }

    public async Task<RentalOrderDetailDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException(
                "Invalid order id.");
        }

        return await _repository.GetByIdAsync(id);
    }




    public async Task<CancelOrderResult> CancelAsync(
        int orderId,
        int customerId)
    {
        if (orderId <= 0 || customerId <= 0)
        {
            throw new ValidationException(
                "Invalid order or customer id.");
        }

        CancelOrderResult result =
            await _repository.CancelAsync(
                orderId,
                customerId);

        if (result == CancelOrderResult.InvalidStatus)
        {
            throw new BusinessRuleException(
                "This order cannot be cancelled in its current status.");
        }

        if (result == CancelOrderResult.Success)
        {
            _logger.LogInformation(
                "Rental order {OrderId} cancelled.",
                orderId);
        }

        return result;
    }

}
