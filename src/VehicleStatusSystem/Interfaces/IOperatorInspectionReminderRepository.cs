
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IOperatorInspectionReminderRepository
{
    Task<InspectionReminderPageDto> GetPageAsync(
        string? status,
        int page,
        int pageSize,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken);

    Task<InspectionReminderDto?> GetByIdAsync(
        long reminderId,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken);
}
