using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync(string? keyword = null, string? status = null);

    Task<Vehicle?> GetByIdAsync(int id);

    Task<Vehicle> AddAsync(Vehicle vehicle);

    Task<bool> UpdateAsync(Vehicle vehicle);

    Task<bool> DeleteAsync(int id);

    Task<bool> PlateNumberExistsAsync(
    string plateNumber,
    int? excludeId = null);
}