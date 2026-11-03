
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/operator/inspection-reminders")]
[Authorize(Roles = "Operator,Admin")]
public sealed class OperatorInspectionRemindersController
    : ControllerBase
{
    private readonly IOperatorInspectionReminderService _service;

    public OperatorInspectionRemindersController(
        IOperatorInspectionReminderService service)
    {
        _service = service;
    }

    private int? GetCurrentUserId()
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int id)
            && id > 0
                ? id
                : null;
    }

    [HttpGet]
    public async Task<ActionResult<InspectionReminderPageDto>>
        GetPage(
            [FromQuery] InspectionReminderQueryDto query,
            CancellationToken cancellationToken)
    {
        int? userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _service.GetPageAsync(
                query,
                userId.Value,
                User.IsInRole("Admin"),
                cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InspectionReminderDto>>
        GetById(
            long id,
            CancellationToken cancellationToken)
    {
        int? userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _service.GetByIdAsync(
                id,
                userId.Value,
                User.IsInRole("Admin"),
                cancellationToken);

            if (result is null)
                return NotFound(new
                {
                    message = "Reminder not found."
                });

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
