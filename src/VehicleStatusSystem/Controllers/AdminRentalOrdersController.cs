
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/admin/rental-orders")]
[Authorize(Roles = "Admin")]
public class AdminRentalOrdersController : ControllerBase
{
    private readonly IAdminRentalOrderService _service;

    private readonly IAdminRentalOrderDetailService
        _detailService;

    public AdminRentalOrdersController(
        IAdminRentalOrderService service,
        IAdminRentalOrderDetailService detailService)
    {
        _service = service;
        _detailService = detailService;
    }

    // V3.4.1：Admin 查询全部订单
    [HttpGet]
    public async Task<ActionResult<AdminRentalOrderPageDto>>
        GetPaged(
            [FromQuery] AdminRentalOrderQueryDto query)
    {
        var result = await _service.GetPagedAsync(query);

        return Ok(result);
    }

    // V3.4.2：Admin 查询订单完整履约详情
    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<AdminRentalOrderDetailDto>>
        GetById(int orderId)
    {
        var result =
            await _detailService.GetByIdAsync(orderId);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Rental order not found."
            });
        }

        return Ok(result);
    }
}
