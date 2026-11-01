
namespace VehicleStatusSystem.DTOs;

public class AvailableVehicleDto
{
    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = "";

    public int ModelId { get; set; }

    public string ModelName { get; set; } = "";

    public decimal DailyRate { get; set; }

    public string Status { get; set; } = "";

    public string OperationStatus { get; set; } = "";
}
