namespace VehicleStatusSystem.DTOs;

public class VehicleDto
{
    public int Id { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; }
}