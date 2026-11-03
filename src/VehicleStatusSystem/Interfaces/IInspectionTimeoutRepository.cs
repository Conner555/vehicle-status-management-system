
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IInspectionTimeoutRepository
{
    Task<IReadOnlyList<int>> GetOverdueInspectionIdsAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<InspectionTimeoutSyncResult> SynchronizeEventsAsync(
        int batchSize,
        CancellationToken cancellationToken);
}
