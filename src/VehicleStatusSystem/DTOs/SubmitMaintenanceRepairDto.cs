
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public sealed class SubmitMaintenanceRepairDto
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string RepairNotes { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal RepairCost { get; set; }
}
