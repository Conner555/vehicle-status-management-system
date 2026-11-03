
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IAdminInspectionTimeoutRepository
{
    Task<InspectionTimeoutPageDto> GetPageAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InspectionTimeoutEventDto?> GetByIdAsync(
        long eventId,
        CancellationToken cancellationToken);
}
