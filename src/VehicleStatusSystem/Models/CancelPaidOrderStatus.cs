namespace VehicleStatusSystem.Models;

public enum CancelPaidOrderStatus
{
    Success,
    AlreadyRefunded,
    IdempotencyConflict,
    NotFound,
    InvalidStatus,
    AlreadyHandedOver,
    PaymentNotFound
}