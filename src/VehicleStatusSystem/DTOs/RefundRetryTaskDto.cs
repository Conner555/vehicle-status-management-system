
namespace VehicleStatusSystem.DTOs;

public sealed class RefundRetryTaskDto
{
    public long Id { get; set; }

    public Guid OperationKey { get; set; }

    public int OrderId { get; set; }

    public int? OrderItemId { get; set; }

    public string Reason { get; set; } = "";

    public decimal Amount { get; set; }

    public string Status { get; set; } = "";

    public int RetryCount { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public string? LastError { get; set; }
}
