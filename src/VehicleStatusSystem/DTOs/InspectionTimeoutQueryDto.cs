
namespace VehicleStatusSystem.DTOs;

public class InspectionTimeoutQueryDto
{
    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
