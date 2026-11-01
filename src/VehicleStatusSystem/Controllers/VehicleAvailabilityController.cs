
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/vehicles/availability")]
public class VehicleAvailabilityController : ControllerBase
{
    private readonly IVehicleAvailabilityService _service;

    public VehicleAvailabilityController(
        IVehicleAvailabilityService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<AvailabilityPageDto>>
        GetAvailable([FromQuery] AvailabilityQueryDto query)
    {
        try
        {
            var result =
                await _service.GetAvailableAsync(query);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = ex.Message
            });
        }
    }
}
