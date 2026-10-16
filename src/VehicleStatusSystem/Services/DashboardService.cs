
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        return await _dashboardRepository.GetSummaryAsync();
    }
}
