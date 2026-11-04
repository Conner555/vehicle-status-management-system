
namespace VehicleStatusSystem.DTOs;

public sealed class InspectionHistoryPageDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<InspectionHistoryDto> Items { get; set; } = new();
}
