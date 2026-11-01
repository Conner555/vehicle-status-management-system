using VehicleStatusSystem.Models;
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IRefundRepository
{
    Task<(CancelPaidOrderStatus Status,
          CancelPaidOrderResultDto? Refund)> CancelPaidOrderAsync(
        int orderId,
        int customerId,
        Guid idempotencyKey);
}