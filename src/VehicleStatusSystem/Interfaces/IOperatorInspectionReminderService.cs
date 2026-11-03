
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IOperatorInspectionReminderService
{
    Task<InspectionReminderPageDto> GetPageAsync(
        InspectionReminderQueryDto query,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken);

    Task<InspectionReminderDto?> GetByIdAsync(
        long reminderId,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken);
}
