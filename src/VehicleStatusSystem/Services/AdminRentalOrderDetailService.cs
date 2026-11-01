
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class AdminRentalOrderDetailService
    : IAdminRentalOrderDetailService
{
    private readonly IAdminRentalOrderDetailRepository _repository;
    private readonly ILogger<AdminRentalOrderDetailService> _logger;

    public AdminRentalOrderDetailService(
        IAdminRentalOrderDetailRepository repository,
        ILogger<AdminRentalOrderDetailService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AdminRentalOrderDetailDto?> GetByIdAsync(
        int orderId)
    {
        if (orderId <= 0)
        {
            throw new ValidationException(
                "Invalid order id.");
        }

        _logger.LogInformation(
            "Admin querying rental order detail: {OrderId}",
            orderId);

        return await _repository.GetByIdAsync(orderId);
    }
}
