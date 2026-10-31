
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentResult> PayAsync(
        int orderId,
        int customerId,
        string method,
        Guid idemjpotencyKey);
}
