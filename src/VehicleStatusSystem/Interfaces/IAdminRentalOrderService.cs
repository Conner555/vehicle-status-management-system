
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Interfaces;

public interface IAdminRentalOrderService
{
    Task<AdminRentalOrderPageDto> GetPagedAsync(
        AdminRentalOrderQueryDto query);
}
