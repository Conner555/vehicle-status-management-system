
using System.ComponentModel.DataAnnotations;

namespace VehicleStatusSystem.DTOs;

public class VehiclePageQueryDto
{
    [StringLength(100)]
    public string? Keyword { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
