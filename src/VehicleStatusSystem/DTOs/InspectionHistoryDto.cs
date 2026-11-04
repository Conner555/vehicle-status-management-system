
namespace VehicleStatusSystem.DTOs;

public sealed class InspectionHistoryDto
{
    public int InspectionId { get; set; }

    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = "";

    public string ModelName { get; set; } = "";

    public string Status { get; set; } = "";

    public DateTime ReturnedAt { get; set; }

    public DateTime? ScheduledInspectionAt { get; set; }

    public DateTime InspectionDeadlineAt { get; set; }

    public DateTime? InspectedAt { get; set; }

    public string? ResultNotes { get; set; }
}
