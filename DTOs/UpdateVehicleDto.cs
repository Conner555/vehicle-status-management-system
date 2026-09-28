using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public class UpdateVehicleDto
{
    [Required]
    [StringLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;
}