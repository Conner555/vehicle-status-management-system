
namespace VehicleStatusSystem.Models;

public enum HandoverResult
{
    Success,
    NotFound,
    InvalidOrderStatus,
    InvalidItemStatus,
    VehicleUnavailable,
    OutsideHandoverWindow
}
