
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public sealed class OperatorInspectionReminderService
    : IOperatorInspectionReminderService
{
    private readonly IOperatorInspectionReminderRepository _repository;

    public OperatorInspectionReminderService(
        IOperatorInspectionReminderRepository repository)
    {
        _repository = repository;
    }

    public async Task<InspectionReminderPageDto> GetPageAsync(
        InspectionReminderQueryDto query,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (operatorId <= 0)
            throw new ValidationException("Invalid operator ID.");

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
            status != "Resolved" &&
            status != "Escalated")
        {
            throw new ValidationException(
                "Status must be Open, Resolved or Escalated.");
        }

        return await _repository.GetPageAsync(
            status,
            query.Page,
            query.PageSize,
            operatorId,
            isAdmin,
            cancellationToken);
    }

    public async Task<InspectionReminderDto?> GetByIdAsync(
        long reminderId,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (reminderId <= 0)
            throw new ValidationException(
                "ReminderId must be positive.");

        if (operatorId <= 0)
            throw new ValidationException(
                "Invalid operator ID.");

        return await _repository.GetByIdAsync(
            reminderId,
            operatorId,
            isAdmin,
            cancellationToken);
    }
}
