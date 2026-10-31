
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IInspectionService
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

}
