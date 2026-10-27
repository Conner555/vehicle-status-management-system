using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IReturnRepository
{
    Task<ReturnVehicleResult> ReturnAsync(
        int orderId,
        int orderItemId,
        int customerId);
}
