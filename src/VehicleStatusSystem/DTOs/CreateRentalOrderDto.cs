
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public class CreateRentalOrderDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(5)]
    public List<CreateRentalOrderItemDto> Items
        { get; set; } = new();
}
