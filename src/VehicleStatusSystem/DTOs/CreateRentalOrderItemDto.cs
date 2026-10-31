
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public class CreateRentalOrderItemDto
{
    [Range(1, int.MaxValue)]
    public int VehicleId { get; set; }

    [Required]
    public DateTimeOffset? StartAt { get; set; }

    [Required]
    public DateTimeOffset? EndAt { get; set; }
}
