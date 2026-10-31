
namespace VehicleStatusSystem.Models;

public sealed record RentalOrderLine(
    int VehicleId,
    DateTime StartAt,
    DateTime EndAt,
    int RentalDays
);
