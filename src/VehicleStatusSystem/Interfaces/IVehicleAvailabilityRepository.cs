
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IVehicleAvailabilityRepository
{
    Task<AvailabilityPageDto> GetAvailableAsync(
        DateTime startAtUtc,
        DateTime endAtUtc,
        int? modelId,
        int page,
        int pageSize);
}
