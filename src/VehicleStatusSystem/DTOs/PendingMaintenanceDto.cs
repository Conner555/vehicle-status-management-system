namespace VehicleStatusSystem.DTOs;

public sealed class PendingMaintenanceDto
{
    public int MaintenanceId { get; set; }

    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public int? InspectionId { get; set; }

    public string IssueDescription { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
