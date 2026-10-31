
namespace VehicleStatusSystem.DTOs;

public sealed class PendingMaintenancePageDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<PendingMaintenanceDto> Items { get; set; } = new();
}
