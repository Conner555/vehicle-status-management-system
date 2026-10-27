using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IRentalOrderRepository
{
    Task<int> CreateAsync(
        int customerId,
        IReadOnlyList<RentalOrderLine> lines);

    Task<RentalOrderDetailDto?> GetByIdAsync(int id);

    Task<CancelOrderResult> CancelAsync(
        int orderId,
        int customerId);

    
    Task<List<int>> GetExpiredOrderIdsAsync(int batchSize);

    Task<bool> ExpireOrderAsync(int orderId);
}
