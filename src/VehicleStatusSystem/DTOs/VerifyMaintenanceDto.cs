
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public sealed class VerifyMaintenanceDto
{
    [Required]
    public bool? Approved { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string VerificationNotes { get; set; } = string.Empty;
}
