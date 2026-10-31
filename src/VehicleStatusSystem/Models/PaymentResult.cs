
namespace VehicleStatusSystem.Models;

public enum PaymentStatus
{
    Succeeded,
    NotFound,
    InvalidOrderStatus,
    Expired,
    AlreadyPaid,
    //下面是关于重复支付的状态
    IdempotentReplay,
    IdempotencyConflict

}

public sealed record PaymentResult(
    PaymentStatus Status,
    Guid? PaymentNo = null,
    decimal? Amount = null
);
