
namespace VehicleStatusSystem.Models;

public enum MaintenanceSubmitResult
{
    Success,
    NotFound,
    NotInProgress,
    Forbidden,
    VehicleStateConflict
}
