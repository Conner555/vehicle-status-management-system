using VehicleStatusSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/rental-orders")]
[Authorize]
public class RentalOrdersController : ControllerBase
{
    private readonly IRentalOrderService _rentalOrderService;

    public RentalOrdersController(
        IRentalOrderService rentalOrderService)
    {
        _rentalOrderService = rentalOrderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRentalOrderDto dto)
    {
        // 1. 从经过验证的 JWT 中获取用户 ID
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

        // 2. 调用 Service 创建订单
        int orderId = await _rentalOrderService
            .CreateAsync(customerId, dto);

        // 3. 返回新订单 ID
        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                id = orderId,
                status = "PendingPayment"
            });
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out int customerId)
            || customerId <= 0)
        {
            return Unauthorized();
        }

        var order = await _rentalOrderService.GetByIdAsync(id);

        if (order is null)
            return NotFound();

        // 普通用户只能查看自己的订单
        bool isAdmin = User.IsInRole("Admin");

        if (!isAdmin && order.CustomerId != customerId)
            return NotFound();

        return Ok(order);
    }




    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out int customerId)
            || customerId <= 0)
        {
            return Unauthorized();
        }

        CancelOrderResult result =
            await _rentalOrderService.CancelAsync(
                id,
                customerId);

        if (result == CancelOrderResult.NotFound)
            return NotFound();

        return NoContent();
    }


}
