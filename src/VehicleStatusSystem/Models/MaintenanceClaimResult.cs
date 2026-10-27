
namespace VehicleStatusSystem.Models;

public enum MaintenanceClaimResult
{
    Success,
    NotFound,
    AlreadyClaimed,
    VehicleStateConflict
}
