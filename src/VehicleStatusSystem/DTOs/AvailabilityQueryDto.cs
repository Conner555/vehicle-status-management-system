
namespace VehicleStatusSystem.DTOs;

public class AvailabilityQueryDto
{
    public DateTimeOffset? StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    public int? ModelId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
