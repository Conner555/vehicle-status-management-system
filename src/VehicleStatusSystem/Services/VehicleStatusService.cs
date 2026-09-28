using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Exceptions;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class VehicleStatusService
    : IVehicleStatusService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleStatusRepository _statusRepository;

    public VehicleStatusService(
        IVehicleRepository vehicleRepository,
        IVehicleStatusRepository statusRepository)
    {
        _vehicleRepository = vehicleRepository;
        _statusRepository = statusRepository;
    }

    public async Task<List<VehicleStatusDto>> GetByVehicleIdAsync(int vehicleId)
    {
        Vehicle? vehicle =
            await _vehicleRepository.GetByIdAsync(vehicleId);

        if (vehicle is null)
        {
            throw new ResourceNotFoundException(
                $"Vehicle {vehicleId} was not found.");
        }

        List<VehicleStatusRecord> records =
            await _statusRepository
                .GetByVehicleIdAsync(vehicleId);

        return records
            .Select(ToDto)
            .ToList();
    }

    public async Task<VehicleStatusDto> CreateAsync(
        int vehicleId,
        CreateVehicleStatusDto dto)
    {
        Vehicle? vehicle =
            await _vehicleRepository.GetByIdAsync(vehicleId);

        if (vehicle is null)
        {
            throw new ResourceNotFoundException(
                $"Vehicle {vehicleId} was not found.");
        }

        VehicleStatusRecord record = new()
        {
            VehicleId = vehicleId,
            Temperature = dto.Temperature,
            Speed = dto.Speed,
            Battery = dto.Battery
        };

        VehicleStatusRecord created =
            await _statusRepository.AddAsync(record);

        return ToDto(created);
    }

    private static VehicleStatusDto ToDto(
        VehicleStatusRecord record)
    {
        return new VehicleStatusDto
        {
            Id = record.Id,
            VehicleId = record.VehicleId,
            Temperature = record.Temperature,
            Speed = record.Speed,
            Battery = record.Battery,
            RecordTime = record.RecordTime
        };
    }
}