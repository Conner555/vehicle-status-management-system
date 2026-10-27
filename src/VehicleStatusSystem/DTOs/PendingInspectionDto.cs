
namespace VehicleStatusSystem.DTOs;

public sealed class PendingInspectionDto
{
    public int InspectionId { get; set; }
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ReturnedAt { get; set; }
    public DateTime InspectionDueAt { get; set; }

    public bool IsDue { get; set; }
}
