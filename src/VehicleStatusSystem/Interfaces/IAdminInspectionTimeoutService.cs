
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IAdminInspectionTimeoutService
{
    Task<InspectionTimeoutPageDto> GetPageAsync(
        InspectionTimeoutQueryDto query,
        CancellationToken cancellationToken);

    Task<InspectionTimeoutEventDto?> GetByIdAsync(
        long eventId,
        CancellationToken cancellationToken);
}
