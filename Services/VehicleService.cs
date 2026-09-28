using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Exceptions;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleStatusRepository _statusRepository;
    private readonly ILogger<VehicleService> _logger;
    

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IVehicleStatusRepository statusRepository,
        ILogger<VehicleService> logger)
    {
        _vehicleRepository = vehicleRepository;
        _statusRepository = statusRepository;
        _logger = logger;
    }

    public async Task<List<VehicleDto>> GetAllAsync()
    {
        List<Vehicle> vehicles =
            await _vehicleRepository.GetAllAsync();

        return vehicles
            .Select(ToDto)
            .ToList();
    }

    public async Task<VehicleDto?> GetByIdAsync(int id)
    {
        Vehicle? vehicle =
            await _vehicleRepository.GetByIdAsync(id);

        if (vehicle is null)
        {
            return null;
        }

        return ToDto(vehicle);
    }

    public async Task<VehicleDto> CreateAsync(
        CreateVehicleDto dto)
    {
        bool exists =
            await _vehicleRepository
                .PlateNumberExistsAsync(dto.PlateNumber);

        if (exists)
        {
            _logger.LogWarning(
                "Vehicle creation rejected because the plate number already exists.");

            throw new BusinessRuleException(
                $"Plate number {dto.PlateNumber} already exists.");
        }

        Vehicle vehicle = new()
        {
            PlateNumber = dto.PlateNumber,
            Model = dto.Model,
            Status = dto.Status
        };

        Vehicle created =
            await _vehicleRepository.AddAsync(vehicle);

        _logger.LogInformation(
            "Vehicle {VehicleId} created successfully.",
            created.Id);

        return ToDto(created);
    }

    public async Task<VehicleDto?> UpdateAsync(
        int id,
        UpdateVehicleDto dto)
    {
        Vehicle? existing =
            await _vehicleRepository.GetByIdAsync(id);

        if (existing is null)
        {
            return null;
        }

        bool plateExists =
            await _vehicleRepository
                .PlateNumberExistsAsync(
                    dto.PlateNumber,
                    id);

        if (plateExists)
        {
            throw new BusinessRuleException(
                $"Plate number {dto.PlateNumber} already exists.");
        }

        existing.PlateNumber = dto.PlateNumber;
        existing.Model = dto.Model;
        existing.Status = dto.Status;

        bool update = await _vehicleRepository.UpdateAsync(existing);

        if(!update)
        {
            return null;
        }

        _logger.LogInformation(
            "Vehicle {VehicleId} update successfully!",
            existing.Id
        );

        return ToDto(existing);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        Vehicle? vehicle =
            await _vehicleRepository.GetByIdAsync(id);

        if (vehicle is null)
        {
            return false;
        }

        bool hasRecords =
            await _statusRepository.HasRecordsAsync(id);

        if (hasRecords)
        {
            throw new BusinessRuleException(
                "The vehicle has status records and cannot be deleted.");
        }

        bool deleted =
            await _vehicleRepository.DeleteAsync(id);

        if (deleted)
        {
            _logger.LogInformation(
                "Vehicle {VehicleId} deleted successfully.",
                id);
        }

        return deleted;
    }

    private static VehicleDto ToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Model = vehicle.Model,
            Status = vehicle.Status,
            CreateTime = vehicle.CreateTime
        };
    }
}