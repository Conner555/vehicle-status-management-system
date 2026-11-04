using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/inspections")]
[Authorize(Roles = "Operator,Admin")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;

    public InspectionController(
        IInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<PendingInspectionPageDto>> GetPending(
        [FromQuery] bool? isDue = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100
            || ((long)page - 1) * pageSize > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "Invalid pagination parameters."
            });
        }

        PendingInspectionPageDto result =
            await _inspectionService.GetPendingAsync(
                isDue,
                page,
                pageSize);

        return Ok(result);
    }



    // 以下方法添加到现有 InspectionController 类内部

    [HttpPost("{id:int}/claim")]
    public async Task<IActionResult> Claim(int id)
    {
        // 1. 从 JWT 获取当前 Operator 的用户 ID
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue,
            out int operatorId) || operatorId <= 0)
        {
            return Unauthorized();
        }

        // 2. 调用 Service 领取任务
        InspectionClaimResult result =
            await _inspectionService.ClaimAsync(
                id,
                operatorId);

        // 3. 映射为 HTTP 状态码
        return result switch
        {
            InspectionClaimResult.Success => Ok(new
            {
                inspectionId = id,
                operatorId,
                status = "InProgress"
            }),

            InspectionClaimResult.NotFound => NotFound(new
            {
                message = "Inspection task not found."
            }),

            InspectionClaimResult.NotDue => Conflict(new
            {
                message = "Inspection is not due yet."
            }),

            InspectionClaimResult.AlreadyClaimed => Conflict(new
            {
                message = "Inspection task has already been claimed."
            }),

            InspectionClaimResult.InvalidItemStatus => Conflict(new
            {
                message = "Rental item is not pending inspection."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown inspection claim result."
            })
        };
    }



    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(
        int id,
        [FromBody] CompleteInspectionDto dto)
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out int operatorId)
            || operatorId <= 0)
        {
            return Unauthorized();
        }

        bool isAdmin = User.IsInRole("Admin");

        InspectionCompleteResult result =
            await _inspectionService.CompleteAsync(
                id,
                operatorId,
                isAdmin,
                dto.Result,
                dto.ResultNotes);

        return result switch
        {
            InspectionCompleteResult.Success => Ok(new
            {
                inspectionId = id,
                status = dto.Result
            }),

            InspectionCompleteResult.NotFound => NotFound(new
            {
                message = "Inspection task not found."
            }),

            InspectionCompleteResult.NotClaimed => Conflict(new
            {
                message = "Inspection task has not been claimed."
            }),

            InspectionCompleteResult.Forbidden => StatusCode(403,
                new { message = "This task belongs to another operator." }),

            InspectionCompleteResult.AlreadyCompleted => Conflict(new
            {
                message = "Order is not in use."
            }),

            InspectionCompleteResult.InvalidItemStatus => Conflict(new
            {
                message = "Rental item is not pending inspection."
            }),

            InspectionCompleteResult.InvalidOrderStatus => Conflict(new
            {
                message = "Rental order is not 'Inuse'."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown inspection completion result."
            })
        };
    }


    [HttpGet("mine")]
    public async Task<ActionResult<MyInspectionPageDto>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue, out int operatorId) || operatorId <= 0)
        {
            return Unauthorized();
        }

        if (page < 1 || pageSize < 1 || pageSize > 100 ||
            ((long)page - 1) * pageSize > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "Invalid pagination parameters."
            });
        }

        var result = await _inspectionService.GetMineAsync(
            operatorId,
            page,
            pageSize);

        return Ok(result);
    }

    //查询历史记录
    [HttpGet("history")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<InspectionHistoryPageDto>> GetHistory(
        [FromQuery] string? result = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // 从 JWT 获取当前 Operator ID
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue,
            out int operatorId) || operatorId <= 0)
        {
            return Unauthorized();
        }

        // 校验分页
        if (page < 1 ||
            pageSize < 1 ||
            pageSize > 100 ||
            ((long)page - 1) * pageSize > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "Invalid pagination parameters."
            });
        }

        if (!string.IsNullOrWhiteSpace(result) &&
            result != "Passed" &&
            result != "Failed")
        {
            return BadRequest(new
            {
                message = "Invalid inspection result."
            });
        }

        var history = await _inspectionService.GetHistoryAsync(
            operatorId,
            result,
            page,
            pageSize);

        return Ok(history);
    }


}
