
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IPaymentService
{
    Task<PaymentResult> PayAsync(
        int orderId,
        int customerId,
        string method,
        Guid idempotencyKey);
}
