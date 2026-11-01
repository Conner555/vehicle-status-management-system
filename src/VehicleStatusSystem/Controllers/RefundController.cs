
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/rental-orders")]
[Authorize(Roles = "User")]
public class RefundController : ControllerBase
{
    private readonly IRefundService _refundService;

    public RefundController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    [HttpPost("{orderId:int}/cancel-paid")]
    public async Task<IActionResult> CancelPaidOrder(
        int orderId,
        [FromHeader(Name = "Idempotency-Key")]
        string? key)
    {
        // 1. 获取 JWT 用户 ID
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

        // 2. 检查幂等键
        if (!Guid.TryParse(key, out Guid idempotencyKey)
            || idempotencyKey == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Valid Idempotency-Key is required."
            });
        }

        // 3. 调用 Service
        var (status, refund) =
            await _refundService.CancelPaidOrderAsync(
                orderId,
                customerId,
                idempotencyKey);

        // 4. 将业务状态转换为 HTTP 响应
        return status switch
        {
            CancelPaidOrderStatus.Success =>
                Ok(refund),

            CancelPaidOrderStatus.NotFound =>
                NotFound(new
                {
                    message = "Order not found."
                }),

            CancelPaidOrderStatus.InvalidStatus =>
                Conflict(new
                {
                    message = "Order status does not allow cancellation."
                }),

            CancelPaidOrderStatus.AlreadyHandedOver =>
                Conflict(new
                {
                    message = "One or more vehicles have already been handed over."
                }),

            CancelPaidOrderStatus.AlreadyRefunded =>
                Conflict(new
                {
                    message = "A refund already exists for this order."
                }),

            CancelPaidOrderStatus.IdempotencyConflict =>
                Conflict(new
                {
                    message = "Idempotency-Key conflict."
                }),

            CancelPaidOrderStatus.PaymentNotFound =>
                Conflict(new
                {
                    message = "Successful payment not found."
                }),

            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Unexpected refund result."
                })
        };
    }
}
