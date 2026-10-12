using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IVehicleService
{
    Task<List<VehicleDto>> GetAllAsync(string? keyword = null, string? status = null);

    Task<VehicleDto?> GetByIdAsync(int id);

    Task<VehicleDto> CreateAsync(
        CreateVehicleDto dto);

    Task<VehicleDto?> UpdateAsync(
        int id,
        UpdateVehicleDto dto);

    Task<bool> DeleteAsync(int id);
}