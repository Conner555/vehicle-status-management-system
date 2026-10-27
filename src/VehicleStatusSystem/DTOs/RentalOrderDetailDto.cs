
namespace VehicleStatusSystem.DTOs;

public class RentalOrderDetailDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public List<RentalOrderItemDetailDto> Items
        { get; set; } = new();
}

public class RentalOrderItemDetailDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal DailyRate { get; set; }
    public int RentalDays { get; set; }
    public decimal Subtotal { get; set; }
    public string Status { get; set; } = "";
}
