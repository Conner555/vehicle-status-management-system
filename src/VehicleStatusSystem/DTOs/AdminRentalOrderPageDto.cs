
namespace VehicleStatusSystem.DTOs;

public class AdminRentalOrderPageDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }

    public List<AdminRentalOrderDto> Items { get; set; } = new();
}
