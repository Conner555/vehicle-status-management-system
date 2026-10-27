
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IReturnService
{
    Task<ReturnVehicleResult> ReturnAsync(
        int orderId,
        int orderItemId,
        int customerId);
}
