
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IVehicleAvailabilityService
{
    Task<AvailabilityPageDto> GetAvailableAsync(
        AvailabilityQueryDto query);
}
