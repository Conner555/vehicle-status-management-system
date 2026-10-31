
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public class MockPaymentDto
{
    [Required]
    public string Method { get; set; } = string.Empty;
}
