
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IHandoverRepository
{
    Task<HandoverResult> HandoverAsync(
        int orderId,
        int orderItemId,
        int operatorId);
}
