
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class HandoverService : IHandoverService
{
    private readonly IHandoverRepository _repository;
    private readonly ILogger<HandoverService> _logger;

    public HandoverService(
        IHandoverRepository repository,
        ILogger<HandoverService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<HandoverResult> HandoverAsync(
        int orderId,
        int orderItemId,
        int operatorId)
    {
        // 1. 验证订单 ID
        if (orderId <= 0)
        {
            throw new ValidationException(
                "Invalid order id.");
        }

        // 2. 验证订单明细 ID
        if (orderItemId <= 0)
        {
            throw new ValidationException(
                "Invalid rental order item id.");
        }

        // 3. 验证业务员 ID
        if (operatorId <= 0)
        {
            throw new ValidationException(
                "Invalid operator id.");
        }

        // 4. 调用 Repository 执行交车事务
        HandoverResult result =
            await _repository.HandoverAsync(
                orderId,
                orderItemId,
                operatorId);

        // 5. 根据业务结果记录日志
        switch (result)
        {
            case HandoverResult.Success:
                _logger.LogInformation(
                    "Vehicle handover succeeded. OrderId={OrderId}, ItemId={ItemId}, OperatorId={OperatorId}",
                    orderId,
                    orderItemId,
                    operatorId);
                break;

            case HandoverResult.NotFound:
                _logger.LogInformation(
                    "Handover target not found. OrderId={OrderId}, ItemId={ItemId}",
                    orderId,
                    orderItemId);
                break;

            case HandoverResult.InvalidOrderStatus:
                _logger.LogWarning(
                    "Handover rejected: invalid order status. OrderId={OrderId}",
                    orderId);
                break;

            case HandoverResult.InvalidItemStatus:
                _logger.LogWarning(
                    "Handover rejected: invalid item status. ItemId={ItemId}",
                    orderItemId);
                break;

            case HandoverResult.VehicleUnavailable:
                _logger.LogWarning(
                    "Handover rejected: vehicle unavailable. ItemId={ItemId}",
                    orderItemId);
                break;

            case HandoverResult.OutsideHandoverWindow:
                _logger.LogWarning(
                    "Handover rejected: outside handover window. ItemId={ItemId}",
                    orderItemId);
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown handover result.");
        }

        return result;
    }
}
