
namespace VehicleStatusSystem.Models;

public sealed record InspectionReminderSyncResult(
    int CreatedCount,
    int ResolvedCount,
    int EscalatedCount);
