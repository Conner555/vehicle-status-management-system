
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IEarlyReturnSettlementRepository
{
    Task<EarlyReturnSettlementData?> GetPreviewDataAsync(
        int orderItemId,
        int customerId);
}
