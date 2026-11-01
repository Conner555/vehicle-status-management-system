
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IEarlyReturnSettlementService
{
    Task<EarlyReturnSettlementDto?> GetPreviewAsync(
        int orderItemId,
        int customerId);
}
