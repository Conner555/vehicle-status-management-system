using VehicleStatusSystem.Models;
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IRentalOrderService
{
    Task<int> CreateAsync(
        int customerId,
        CreateRentalOrderDto dto);

    Task<RentalOrderDetailDto?> GetByIdAsync(int id);

    Task<CancelOrderResult> CancelAsync(
        int orderId,
        int customerId);
}
