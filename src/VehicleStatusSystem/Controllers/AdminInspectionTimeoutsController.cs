
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/admin/inspection-timeouts")]
[Authorize(Roles = "Admin")]
public sealed class AdminInspectionTimeoutsController
    : ControllerBase
{
    private readonly IAdminInspectionTimeoutService _service;

    public AdminInspectionTimeoutsController(
        IAdminInspectionTimeoutService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<InspectionTimeoutPageDto>>
        GetPage(
            [FromQuery] InspectionTimeoutQueryDto query,
            CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetPageAsync(
                query,
                cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InspectionTimeoutEventDto>>
        GetById(
            long id,
            CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetByIdAsync(
                id,
                cancellationToken);

            if (result is null)
                return NotFound(new
                {
                    message = "Timeout event not found."
                });

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
