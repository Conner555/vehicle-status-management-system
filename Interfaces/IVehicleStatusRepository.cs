using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IVehicleStatusRepository
{
    Task<List<VehicleStatusRecord>> GetByVehicleIdAsync(int vehicleId);

    Task<VehicleStatusRecord> AddAsync(VehicleStatusRecord record);

    Task<bool> HasRecordsAsync(int vehicleId);
}