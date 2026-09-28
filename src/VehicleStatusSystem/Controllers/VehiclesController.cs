using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

using Microsoft.AspNetCore.Authorization;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(
        IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>>
        GetAll()
    {
        List<VehicleDto> vehicles =
            await _vehicleService.GetAllAsync();

        return Ok(vehicles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleDto>>
        GetById(int id)
    {
        VehicleDto? vehicle =
            await _vehicleService.GetByIdAsync(id);

        if (vehicle is null)
        {
            return NotFound();
        }

        return Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<VehicleDto>>
        Create(CreateVehicleDto dto)
    {
        VehicleDto vehicle =
            await _vehicleService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = vehicle.Id },
            vehicle);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<VehicleDto>>
        Update(
            int id,
            UpdateVehicleDto dto)
    {
        VehicleDto? vehicle =
            await _vehicleService.UpdateAsync(
                id,
                dto);

        if (vehicle is null)
        {
            return NotFound();
        }

        return Ok(vehicle);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        bool deleted =
            await _vehicleService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}