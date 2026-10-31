
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IHandoverService
{
    Task<HandoverResult> HandoverAsync(
        int orderId,
        int orderItemId,
        int operatorId);
}
