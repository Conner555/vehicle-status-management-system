
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IRefundRetryRepository
{
    Task<long> RecordAsync(
        Guid operationKey,
        int orderId,
        int? orderItemId,
        string reason,
        decimal amount,
        string error,
        CancellationToken cancellationToken);

    Task<RefundRetryTaskDto?> GetByIdAsync(
        long taskId,
        CancellationToken cancellationToken);
}
