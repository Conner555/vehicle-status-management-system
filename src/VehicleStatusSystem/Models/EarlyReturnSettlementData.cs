
namespace VehicleStatusSystem.Models;

public class EarlyReturnSettlementData
{
    public int OrderId { get; set; }
    public int OrderItemId { get; set; }
    public int CustomerId { get; set; }

    public int BookedDays { get; set; }
    public decimal DailyRate { get; set; }
    public decimal Subtotal { get; set; }

    public DateTime? ActualHandedOverAt { get; set; }
    public DateTime? ActualReturnedAt { get; set; }

    public string InspectionStatus { get; set; } = "NotCreated";
    public string? RefundStatus { get; set; }
}
