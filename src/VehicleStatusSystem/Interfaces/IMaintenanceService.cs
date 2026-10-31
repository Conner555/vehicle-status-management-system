
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IMaintenanceService
{
    Task<PendingMaintenancePageDto> GetPendingAsync(
        int page,
        int pageSize);

    Task<MaintenanceClaimResult> ClaimAsync(
        int maintenanceId,
        int operatorId);


    Task<MaintenanceSubmitResult> SubmitRepairAsync(
        int maintenanceId,
        int operatorId,
        string repairNotes,
        decimal repairCost);



    Task<MaintenanceVerifyResult> VerifyAsync(
        int maintenanceId,
        int verifierId,
        bool approved,
        string verificationNotes);

}
