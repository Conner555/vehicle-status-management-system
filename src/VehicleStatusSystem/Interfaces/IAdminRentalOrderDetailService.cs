
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IAdminRentalOrderDetailService
{
    Task<AdminRentalOrderDetailDto?> GetByIdAsync(int orderId);
}
