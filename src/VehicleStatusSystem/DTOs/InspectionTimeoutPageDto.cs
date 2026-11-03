
namespace VehicleStatusSystem.DTOs;

public class InspectionTimeoutPageDto
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<InspectionTimeoutEventDto> Items { get; set; }
        = new();
}
