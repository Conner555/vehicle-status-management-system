
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _repository;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        IMaintenanceRepository repository,
        ILogger<MaintenanceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // 1. 查询维修任务
    public async Task<PendingMaintenancePageDto> GetPendingAsync(
        int page,
        int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException(
                "Invalid pagination parameters.");
        }

        long offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            throw new ValidationException(
                "Pagination offset is too large.");
        }

        PendingMaintenancePageDto result =
            await _repository.GetPendingAsync(page, pageSize);

        _logger.LogInformation(
            "Pending maintenances queried. Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            page,
            pageSize,
            result.TotalCount);

        return result;
    }

    // 2. Operator 领取维修任务
    public async Task<MaintenanceClaimResult> ClaimAsync(
        int maintenanceId,
        int operatorId)
    {
        if (maintenanceId <= 0)
        {
            throw new ValidationException(
                "Invalid maintenance id.");
        }

        if (operatorId <= 0)
        {
            throw new ValidationException(
                "Invalid operator id.");
        }

        MaintenanceClaimResult result =
            await _repository.ClaimAsync(
                maintenanceId,
                operatorId);

        switch (result)
        {
            case MaintenanceClaimResult.Success:
                _logger.LogInformation(
                    "Maintenance {MaintenanceId} claimed by operator {OperatorId}.",
                    maintenanceId,
                    operatorId);
                break;

            case MaintenanceClaimResult.NotFound:
                _logger.LogInformation(
                    "Maintenance {MaintenanceId} not found.",
                    maintenanceId);
                break;

            case MaintenanceClaimResult.AlreadyClaimed:
                _logger.LogInformation(
                    "Maintenance {MaintenanceId} has already been claimed.",
                    maintenanceId);
                break;

            case MaintenanceClaimResult.VehicleStateConflict:
                _logger.LogWarning(
                    "Maintenance {MaintenanceId} cannot be claimed because vehicle state is invalid.",
                    maintenanceId);
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown maintenance claim result.");
        }

        return result;
    }


    //operator提交维修结果service
    public async Task<MaintenanceSubmitResult> SubmitRepairAsync(
        int maintenanceId,
        int operatorId,
        string repairNotes,
        decimal repairCost)
    {
        // 1. 参数检查
        if (maintenanceId <= 0 || operatorId <= 0)
            throw new ValidationException(
                "Invalid maintenance or operator id.");

        // 2. 标准化维修说明
        string notes = repairNotes?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(notes))
            throw new ValidationException(
                "Repair notes are required.");

        if (notes.Length > 1000)
            throw new ValidationException(
                "Repair notes exceed 1000 characters.");

        if (repairCost < 0 || repairCost > 9999999999.99m ||
            decimal.Round(repairCost, 2) != repairCost)
            throw new ValidationException(
                "Repair cost must be a valid non-negative amount with at most two decimal places.");

        // 3. 调用 Repository
        MaintenanceSubmitResult result =
            await _repository.SubmitRepairAsync(
                maintenanceId,
                operatorId,
                notes,
                repairCost);

        // 4. 业务日志
        _logger.LogInformation(
            "Repair submission processed. MaintenanceId={MaintenanceId}, OperatorId={OperatorId}, Result={Result}",
            maintenanceId,
            operatorId,
            result);

        return result;
    }


    //管理员复核任务
    public async Task<MaintenanceVerifyResult> VerifyAsync(
        int maintenanceId,
        int verifierId,
        bool approved,
        string verificationNotes)
    {
        if (maintenanceId <= 0 || verifierId <= 0)
            throw new ValidationException(
                "Invalid maintenance or verifier id.");

        string notes = verificationNotes?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(notes))
            throw new ValidationException(
                "Verification notes are required.");

        if (notes.Length > 1000)
            throw new ValidationException(
                "Verification notes exceed 1000 characters.");

        MaintenanceVerifyResult result =
            await _repository.VerifyAsync(
                maintenanceId,
                verifierId,
                approved,
                notes);

        _logger.LogInformation(
            "Maintenance verification. MaintenanceId={MaintenanceId}, VerifierId={VerifierId}, Approved={Approved}, Outcome={Outcome}",
            maintenanceId,
            verifierId,
            approved,
            result);

        return result;
    }


}
