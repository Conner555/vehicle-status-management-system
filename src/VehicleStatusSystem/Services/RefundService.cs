
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class RefundService : IRefundService
{
    private readonly IRefundRepository _repository;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        IRefundRepository repository,
        ILogger<RefundService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(CancelPaidOrderStatus Status,
        CancelPaidOrderResultDto? Refund)> CancelPaidOrderAsync(
        int orderId,
        int customerId,
        Guid idempotencyKey)
    {
        if (orderId <= 0)
        {
            throw new ValidationException(
                "Invalid order id.");
        }

        if (customerId <= 0)
        {
            throw new ValidationException(
                "Invalid customer id.");
        }

        if (idempotencyKey == Guid.Empty)
        {
            throw new ValidationException(
                "Idempotency-Key cannot be empty.");
        }

        _logger.LogInformation(
            "User {CustomerId} requests cancellation refund for order {OrderId}",
            customerId,
            orderId);

        return await _repository.CancelPaidOrderAsync(
            orderId,
            customerId,
            idempotencyKey);
    }
}
