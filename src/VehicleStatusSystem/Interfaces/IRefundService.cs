
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IRefundService
{
    Task<(CancelPaidOrderStatus Status,
          CancelPaidOrderResultDto? Refund)> CancelPaidOrderAsync(
        int orderId,
        int customerId,
        Guid idempotencyKey);
}
