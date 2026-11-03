
namespace VehicleStatusSystem.DTOs;

public sealed class InspectionReminderQueryDto
{
    public string? Status { get; set; } = "Open";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
