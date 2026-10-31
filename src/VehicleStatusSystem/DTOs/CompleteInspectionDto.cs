
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public sealed class CompleteInspectionDto
{
    [Required]
    [RegularExpression("^(Passed|Failed)$")]
    public string Result { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ResultNotes { get; set; }
}
