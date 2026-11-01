namespace VehicleStatusSystem.DTOs;

public class AdminRentalOrderQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Status { get; set; }
    public int? CustomerId { get; set; }

    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}
