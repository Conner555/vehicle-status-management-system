
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IAdminRentalOrderRepository
{
    Task<AdminRentalOrderPageDto> GetPagedAsync(
        AdminRentalOrderQueryDto query);
}
