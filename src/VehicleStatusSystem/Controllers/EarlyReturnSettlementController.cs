
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/rental-order-items")]
[Authorize(Roles = "User")]
public class EarlyReturnSettlementController : ControllerBase
{
    private readonly IEarlyReturnSettlementService _service;

    public EarlyReturnSettlementController(
        IEarlyReturnSettlementService service)
    {
        _service = service;
    }

    [HttpGet("{orderItemId:int}/settlement-preview")]
    public async Task<ActionResult<EarlyReturnSettlementDto>>
        GetPreview(int orderItemId)
    {
        // 1. 获取 JWT 中的用户 ID
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out int customerId)
            || customerId <= 0)
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        // 2. 调用 Service
        var result = await _service.GetPreviewAsync(
            orderItemId,
            customerId);

        // 3. 订单明细不存在或不属于当前用户
        if (result is null)
        {
            return NotFound(new
            {
                message = "Order item not found."
            });
        }

        // 4. 返回结算预览
        return Ok(result);
    }
}
