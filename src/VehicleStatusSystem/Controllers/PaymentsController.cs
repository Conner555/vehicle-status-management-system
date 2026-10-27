
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/rental-orders")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> Pay(
        int id,
        [FromBody] MockPaymentDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? key)
    //这里FromBody——具体指HTTP JSON 请求体
    //FromRoute——URL 路径参数
    //FromHeader——HTTP 请求头（这里把Idempotency-key作为值传入key）
    //FromQuery——URL 查询字符串
    {
        // 1. 从 JWT 获取当前登录用户
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out int customerId)
            || customerId <= 0)
        {
            return Unauthorized();
        }

        // 2. 验证客户端传入的幂等键
        if (!Guid.TryParse(key, out Guid idempotencyKey)
            || idempotencyKey == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid Idempotency-Key is required."
            });
        }

        // 3. 调用 Service 执行支付或重放
        PaymentResult result =
            await _paymentService.PayAsync(
                id,
                customerId,
                dto.Method,
                idempotencyKey);

        // 4. 将支付结果转换成 HTTP 响应
        return result.Status switch
        {
            PaymentStatus.Succeeded => Ok(new
            {
                orderId = id,
                paymentNo = result.PaymentNo,
                amount = result.Amount,
                status = "Succeeded"
            }),

            PaymentStatus.IdempotentReplay => Ok(new
            {
                orderId = id,
                paymentNo = result.PaymentNo,
                amount = result.Amount,
                status = "Succeeded"
            }),

            PaymentStatus.IdempotencyConflict => Conflict(new
            {
                message = "Idempotency key was used with different payment parameters."
            }),

            PaymentStatus.AlreadyPaid => Conflict(new
            {
                message = "Order has already been paid."
            }),

            PaymentStatus.Expired => Conflict(new
            {
                message = "Order payment has expired."
            }),

            PaymentStatus.InvalidOrderStatus => Conflict(new
            {
                message = "Order cannot be paid in its current status."
            }),

            PaymentStatus.NotFound => NotFound(new
            {
                message = "Order not found."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown payment result."
            })
        };
    }
}
