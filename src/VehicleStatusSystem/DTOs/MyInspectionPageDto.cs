
namespace VehicleStatusSystem.DTOs;

public sealed class MyInspectionPageDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<MyInspectionDto> Items { get; set; } = new();
}
