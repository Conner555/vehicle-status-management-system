namespace VehicleStatusSystem.DTOs;

public class AdminRentalOrderDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";

    public string OrderStatus { get; set; } = "";
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public int VehicleCount { get; set; }
    public string? PaymentStatus { get; set; }
}
