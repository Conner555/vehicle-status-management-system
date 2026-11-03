
namespace VehicleStatusSystem.DTOs;

public class InspectionTimeoutEventDto
{
    public long Id { get; set; }

    public int InspectionId { get; set; }

    public int OrderItemId { get; set; }

    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = "";

    public string InspectionStatus { get; set; } = "";

    public DateTime DeadlineAt { get; set; }

    public DateTime DetectedAt { get; set; }

    public string Status { get; set; } = "";

    public DateTime? ResolvedAt { get; set; }
}
