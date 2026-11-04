
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IInspectionRepository
{
    Task<PendingInspectionPageDto> GetPendingAsync(
        bool? isDue,
        int page,
        int pageSize);

    Task<InspectionClaimResult> ClaimAsync(
        int inspectionId,
        int operatorId);



    Task<InspectionCompleteResult> CompleteAsync(
        int inspectionId,
        int operatorId,
        bool isAdmin,
        string result,
        string? resultNotes);

    Task<MyInspectionPageDto> GetMineAsync(
        int operatorId,
        int page,
        int pageSize);

    Task<InspectionHistoryPageDto> GetHistoryAsync(
        int operatorId,
        string? result,
        int page,
        int pageSize);

}
