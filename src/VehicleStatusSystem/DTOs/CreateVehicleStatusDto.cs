using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public class CreateVehicleStatusDto
{
    [Range(-100, 300)]
    public decimal Temperature { get; set; }

    [Range(0, 500)]
    public decimal Speed { get; set; }

    [Range(0, 100)]
    public decimal Battery { get; set; }
}