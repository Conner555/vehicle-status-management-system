
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IAdminRentalOrderDetailRepository
{
    Task<AdminRentalOrderDetailDto?> GetByIdAsync(int orderId);
}
