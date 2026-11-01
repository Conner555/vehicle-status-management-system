
namespace VehicleStatusSystem.DTOs;

public class CancelPaidOrderResultDto
{
    public int OrderId { get; set; }

    public long RefundId { get; set; }
    public Guid RefundNo { get; set; }

    public decimal RefundAmount { get; set; }

    public string RefundStatus { get; set; } = "";
    public string OrderStatus { get; set; } = "";
}
