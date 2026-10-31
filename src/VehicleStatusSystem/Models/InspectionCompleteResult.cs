
namespace VehicleStatusSystem.Models;

public enum InspectionCompleteResult
{
    Success,
    NotFound,
    NotClaimed,
    Forbidden,
    AlreadyCompleted,
    InvalidItemStatus,
    InvalidOrderStatus
}
