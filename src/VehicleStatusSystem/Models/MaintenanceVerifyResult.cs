
namespace VehicleStatusSystem.Models;

public enum MaintenanceVerifyResult
{
    Approved,
    Rejected,
    NotFound,
    InvalidStatus,
    Forbidden,
    VehicleStateConflict
}
