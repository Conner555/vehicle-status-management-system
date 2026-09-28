using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IVehicleStatusService
{
    Task<List<VehicleStatusDto>>
        GetByVehicleIdAsync(int vehicleId);

    Task<VehicleStatusDto> CreateAsync(
        int vehicleId,
        CreateVehicleStatusDto dto);
}