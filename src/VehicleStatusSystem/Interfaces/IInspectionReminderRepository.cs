
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IInspectionReminderRepository
{
    Task<InspectionReminderSyncResult> SynchronizeAsync(
        CancellationToken cancellationToken);
}
