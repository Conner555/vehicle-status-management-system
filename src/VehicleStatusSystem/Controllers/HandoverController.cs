
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/rental-orders")]
[Authorize(Roles = "Operator,Admin")]
public class HandoverController : ControllerBase
{
    private readonly IHandoverService _handoverService;

    public HandoverController(
        IHandoverService handoverService)
    {
        _handoverService = handoverService;
    }

    [HttpPost("{orderId:int}/items/{itemId:int}/handover")]
    public async Task<IActionResult> Handover(
        int orderId,
        int itemId)
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out int operatorId)
            || operatorId <= 0)
        {
            return Unauthorized();
        }

        HandoverResult result =
            await _handoverService.HandoverAsync(
                orderId,
                itemId,
                operatorId);

        return result switch
        {
            HandoverResult.Success => Ok(new
            {
                orderId,
                itemId,
                status = "InUse"
            }),

            HandoverResult.NotFound => NotFound(new
            {
                message = "Rental order item not found."
            }),

            HandoverResult.InvalidOrderStatus => Conflict(new
            {
                message = "Order is not ready for handover."
            }),

            HandoverResult.InvalidItemStatus => Conflict(new
            {
                message = "Rental item cannot be handed over."
            }),

            HandoverResult.VehicleUnavailable => Conflict(new
            {
                message = "Vehicle is unavailable."
            }),

            HandoverResult.OutsideHandoverWindow => Conflict(new
            {
                message = "Outside the allowed handover window."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown handover result."
            })
        };
    }
}
