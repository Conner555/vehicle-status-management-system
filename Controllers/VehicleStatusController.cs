using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:int}/status")]
public class VehicleStatusController : ControllerBase
{
    private readonly IVehicleStatusService _statusService;

    public VehicleStatusController(
        IVehicleStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleStatusDto>>>
        GetByVehicleId(int vehicleId)
    {
        List<VehicleStatusDto> records =
            await _statusService
                .GetByVehicleIdAsync(vehicleId);

        return Ok(records);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleStatusDto>>
        Create(
            int vehicleId,
            CreateVehicleStatusDto dto)
    {
        VehicleStatusDto created =
            await _statusService.CreateAsync(
                vehicleId,
                dto);

        return StatusCode(
            StatusCodes.Status201Created,
            created);
    }
}