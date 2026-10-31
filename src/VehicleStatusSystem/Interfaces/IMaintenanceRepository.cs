using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IMaintenanceRepository
{
    // 查询待维修任务
    Task<PendingMaintenancePageDto> GetPendingAsync(
        int page,
        int pageSize);

    // Operator 领取维修任务
    Task<MaintenanceClaimResult> ClaimAsync(
        int maintenanceId,
        int operatorId);

    //operator提交维修结果
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
