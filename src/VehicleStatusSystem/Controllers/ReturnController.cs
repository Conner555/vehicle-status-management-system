
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/rental-orders")]
[Authorize(Roles = "User,Admin")]
public class ReturnController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    [HttpPost("{orderId:int}/items/{itemId:int}/return")]
    public async Task<IActionResult> ReturnVehicle(
        int orderId,
        int itemId)
    {
        // 1. 从 JWT 获取当前用户 Id
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue,
            out int customerId) || customerId <= 0)
        {
            return Unauthorized();
        }

        // 2. 调用 Service 完成车辆归还业务
        ReturnVehicleResult result =
            await _returnService.ReturnAsync(
                orderId,
                itemId,
                customerId);

        // 3. 将业务结果映射成 HTTP 状态码
        return result switch
        {
            ReturnVehicleResult.Success => Ok(new
            {
                orderId,
                itemId,
                status = "PendingInspection"
            }),

            ReturnVehicleResult.NotFound => NotFound(new
            {
                message = "Rental order item not found."
            }),

            ReturnVehicleResult.InvalidOrderStatus => Conflict(new
            {
                message = "Order is not in use."
            }),

            ReturnVehicleResult.InvalidItemStatus => Conflict(new
            {
                message = "Vehicle cannot be returned in its current status."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown vehicle return result."
            })
        };
    }
}
