
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public sealed class AdminInspectionTimeoutService
    : IAdminInspectionTimeoutService
{
    private readonly IAdminInspectionTimeoutRepository _repository;

    public AdminInspectionTimeoutService(
        IAdminInspectionTimeoutRepository repository)
    {
        _repository = repository;
    }

    public async Task<InspectionTimeoutPageDto> GetPageAsync(
        InspectionTimeoutQueryDto query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            throw new ValidationException(
                "Page must be greater than zero.");

        if (query.PageSize is < 1 or > 100)
            throw new ValidationException(
                "PageSize must be between 1 and 100.");

        long offset =
            ((long)query.Page - 1) * query.PageSize;

        if (offset > int.MaxValue)
            throw new ValidationException(
                "Pagination offset is too large.");

        string? status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim();

        if (status is not null &&
            status != "Open" &&
            status != "Resolved")
        {
            throw new ValidationException(
                "Status must be Open or Resolved.");
        }

        return await _repository.GetPageAsync(
            status,
            query.Page,
            query.PageSize,
            cancellationToken);
    }

    public async Task<InspectionTimeoutEventDto?> GetByIdAsync(
        long eventId,
        CancellationToken cancellationToken)
    {
        if (eventId <= 0)
            throw new ValidationException(
                "EventId must be positive.");

        return await _repository.GetByIdAsync(
            eventId,
            cancellationToken);
    }
}
