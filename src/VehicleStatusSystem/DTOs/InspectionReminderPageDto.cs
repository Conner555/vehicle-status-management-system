
namespace VehicleStatusSystem.DTOs;

public sealed class InspectionReminderPageDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<InspectionReminderDto> Items { get; set; } = new();
}
