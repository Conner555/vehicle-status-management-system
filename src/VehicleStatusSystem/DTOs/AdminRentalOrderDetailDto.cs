
namespace VehicleStatusSystem.DTOs;

public class AdminRentalOrderDetailDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";

    public string OrderStatus { get; set; } = "";
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public List<AdminOrderItemDetailDto> Items { get; set; } = new();
    public List<AdminOrderPaymentDto> Payments { get; set; } = new();

    public List<AdminOrderRefundDto> Refunds { get; set; } = new();

}

public class AdminOrderItemDetailDto
{
    public int ItemId { get; set; }
    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = "";
    public string ModelName { get; set; } = "";

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime? ActualReturnedAt { get; set; }

    public decimal DailyRate { get; set; }
    public int RentalDays { get; set; }
    public decimal Subtotal { get; set; }

    public string ItemStatus { get; set; } = "";
    public string OperationStatus { get; set; } = "";

    public AdminOrderInspectionDto? Inspection { get; set; }
    public AdminOrderMaintenanceDto? Maintenance { get; set; }
}

public class AdminOrderInspectionDto
{
    public int InspectionId { get; set; }
    public string Status { get; set; } = "";

    public DateTime ReturnedAt { get; set; }
    public DateTime InspectionDueAt { get; set; }
    public DateTime? InspectedAt { get; set; }

    public string? ResultNotes { get; set; }
}

public class AdminOrderMaintenanceDto
{
    public int MaintenanceId { get; set; }
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";

    public string? RepairNotes { get; set; }
    public decimal? RepairCost { get; set; }

    public int? AssignedTo { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public string? VerificationNotes { get; set; }
}

public class AdminOrderPaymentDto
{
    public long PaymentId { get; set; }
    public string PaymentNo { get; set; } = "";

    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string PaymentStatus { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}



public class AdminOrderRefundDto
{
    public long RefundId { get; set; }

    public string RefundNo { get; set; } = "";

    public int OrderId { get; set; }

    public int? OrderItemId { get; set; }

    public long PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = "";

    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime? RefundedAt { get; set; }
}
