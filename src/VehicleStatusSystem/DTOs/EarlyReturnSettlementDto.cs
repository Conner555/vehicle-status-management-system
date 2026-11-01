
namespace VehicleStatusSystem.DTOs;

public class EarlyReturnSettlementDto
{
    public int OrderId { get; set; }
    public int OrderItemId { get; set; }

    public int BookedDays { get; set; }
    public int ChargedDays { get; set; }

    public decimal OriginalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal RefundAmount { get; set; }

    public string InspectionStatus { get; set; } = "";

    public string SettlementStatus { get; set; } = "";
}
