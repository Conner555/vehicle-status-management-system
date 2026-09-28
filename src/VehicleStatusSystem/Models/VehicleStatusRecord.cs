namespace VehicleStatusSystem.Models;

public class VehicleStatusRecord
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public decimal Temperature { get; set; }

    public decimal Speed { get; set; }

    public decimal Battery { get; set; }

    public DateTime RecordTime { get; set; }
}