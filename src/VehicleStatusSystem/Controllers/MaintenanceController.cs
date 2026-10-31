
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Controllers;

[ApiController]
[Route("api/maintenances")]
[Authorize(Roles = "Operator,Admin")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(
        IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    // Task 4.1：查询待维修任务
    [HttpGet("pending")]
    public async Task<ActionResult<PendingMaintenancePageDto>>
        GetPending(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
    {
        // 1. 校验分页参数
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

        // 2. 调用 Service
        PendingMaintenancePageDto result =
            await _maintenanceService.GetPendingAsync(
                page,
                pageSize);

        // 3. 返回分页结果
        return Ok(result);
    }

    // Task 4.2：领取维修任务
    [HttpPost("{id:int}/claim")]
    public async Task<IActionResult> Claim(int id)
    {
        // 1. 校验维修任务 ID
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Invalid maintenance id."
            });
        }

        // 2. 从 JWT 获取当前业务员 ID
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue,
            out int operatorId) || operatorId <= 0)
        {
            return Unauthorized();
        }

        // 3. 调用 Service 执行领取事务
        MaintenanceClaimResult result =
            await _maintenanceService.ClaimAsync(
                id,
                operatorId);

        // 4. 将业务结果映射为 HTTP 响应
        return result switch
        {
            MaintenanceClaimResult.Success => Ok(new
            {
                maintenanceId = id,
                assignedTo = operatorId,
                status = "InProgress"
            }),

            MaintenanceClaimResult.NotFound => NotFound(new
            {
                message = "Maintenance task not found."
            }),

            MaintenanceClaimResult.AlreadyClaimed => Conflict(new
            {
                message = "Maintenance task has already been claimed."
            }),

            MaintenanceClaimResult.VehicleStateConflict => Conflict(new
            {
                message = "Vehicle is not in maintenance state."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown maintenance claim result."
            })
        };
    }

    //发送operator提交维修结果http
    [HttpPost("{id:int}/submit-repair")]
    public async Task<IActionResult> SubmitRepair(
        int id,
        [FromBody] SubmitMaintenanceRepairDto dto)
    {
        if (id <= 0)
            return BadRequest(new
            {
                message = "Invalid maintenance id."
            });

        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue, out int operatorId) ||
            operatorId <= 0)
            return Unauthorized();

        MaintenanceSubmitResult result =
            await _maintenanceService.SubmitRepairAsync(
                id,
                operatorId,
                dto.RepairNotes,
                dto.RepairCost);

        return result switch
        {
            MaintenanceSubmitResult.Success => Ok(new
            {
                maintenanceId = id,
                status = "AwaitingVerification"
            }),

            MaintenanceSubmitResult.NotFound => NotFound(new
            {
                message = "Maintenance task not found."
            }),

            MaintenanceSubmitResult.NotInProgress => Conflict(new
            {
                message = "Maintenance is not in progress."
            }),

            MaintenanceSubmitResult.Forbidden => StatusCode(
                403,
                new
                {
                    message = "Maintenance belongs to another operator."
                }),

            MaintenanceSubmitResult.VehicleStateConflict => Conflict(new
            {
                message = "Vehicle is not in maintenance state."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown maintenance submission result."
            })
        };
    }

    //admin管理员复核任务
    [HttpPost("{id:int}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Verify(
        int id,
        [FromBody] VerifyMaintenanceDto dto)
    {
        if (id <= 0)
            return BadRequest(new
            {
                message = "Invalid maintenance id."
            });

        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out int verifierId)
            || verifierId <= 0)
        {
            return Unauthorized();
        }

        if (!dto.Approved.HasValue)
            return BadRequest(new
            {
                message = "Approved is required."
            });

        MaintenanceVerifyResult result =
            await _maintenanceService.VerifyAsync(
                id,
                verifierId,
                dto.Approved.Value,
                dto.VerificationNotes);

        return result switch
        {
            MaintenanceVerifyResult.Approved => Ok(new
            {
                maintenanceId = id,
                status = "Completed",
                operationStatus = "Available"
            }),

            MaintenanceVerifyResult.Rejected => Ok(new
            {
                maintenanceId = id,
                status = "InProgress",
                operationStatus = "Maintenance"
            }),

            MaintenanceVerifyResult.NotFound => NotFound(new
            {
                message = "Maintenance task not found."
            }),

            MaintenanceVerifyResult.InvalidStatus => Conflict(new
            {
                message = "Maintenance is not awaiting verification."
            }),

            MaintenanceVerifyResult.Forbidden =>
                StatusCode(403, new
                {
                    message = "Repair operator cannot verify own task."
                }),

            MaintenanceVerifyResult.VehicleStateConflict => Conflict(new
            {
                message = "Vehicle cannot be restored to operation."
            }),

            _ => StatusCode(500, new
            {
                message = "Unknown maintenance verification result."
            })
        };
    }


}
