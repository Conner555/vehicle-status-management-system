
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class ReturnService : IReturnService
{
    private readonly IReturnRepository _repository;
    private readonly ILogger<ReturnService> _logger;

    public ReturnService(
        IReturnRepository repository,
        ILogger<ReturnService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ReturnVehicleResult> ReturnAsync(
        int orderId,
        int orderItemId,
        int customerId)
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

        // 3. 验证客户 ID
        if (customerId <= 0)
        {
            throw new ValidationException(
                "Invalid customer id.");
        }

        // 4. 调用 Repository 执行归还事务
        ReturnVehicleResult result =
            await _repository.ReturnAsync(
                orderId,
                orderItemId,
                customerId);

        // 5. 根据结果记录日志
        switch (result)
        {
            case ReturnVehicleResult.Success:
                _logger.LogInformation(
                    "Vehicle returned successfully. OrderId={OrderId}, ItemId={ItemId}, CustomerId={CustomerId}",
                    orderId,
                    orderItemId,
                    customerId);
                break;

            case ReturnVehicleResult.NotFound:
                _logger.LogInformation(
                    "Return target not found. OrderId={OrderId}, ItemId={ItemId}",
                    orderId,
                    orderItemId);
                break;

            case ReturnVehicleResult.InvalidOrderStatus:
                _logger.LogWarning(
                    "Vehicle return rejected: invalid order status. OrderId={OrderId}",
                    orderId);
                break;

            case ReturnVehicleResult.InvalidItemStatus:
                _logger.LogWarning(
                    "Vehicle return rejected: invalid item status. ItemId={ItemId}",
                    orderItemId);
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown vehicle return result.");
        }

        return result;
    }
}
