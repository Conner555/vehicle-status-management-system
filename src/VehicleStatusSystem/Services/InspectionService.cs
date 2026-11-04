using VehicleStatusSystem.Models;
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class InspectionService : IInspectionService
{
    private readonly IInspectionRepository _repository;
    private readonly ILogger<InspectionService> _logger;

    public InspectionService(
        IInspectionRepository repository,
        ILogger<InspectionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MyInspectionPageDto> GetMineAsync(
        int operatorId,
        int page,
        int pageSize)
    {
        if (operatorId <= 0)
        {
            throw new ValidationException(
                "Invalid operator id.");
        }

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

        return await _repository.GetMineAsync(
            operatorId,
            page,
            pageSize);
    }

    public async Task<InspectionHistoryPageDto> GetHistoryAsync(
        int operatorId,
        string? result,
        int page,
        int pageSize)
    {
        if (operatorId <= 0)
        {
            throw new ValidationException(
                "Invalid operator id.");
        }

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

        // 规范化筛选参数
        string? normalizedResult =
            string.IsNullOrWhiteSpace(result)
                ? null
                : result.Trim();

        if (normalizedResult is not null &&
            normalizedResult != "Passed" &&
            normalizedResult != "Failed")
        {
            throw new ValidationException(
                "Result must be Passed or Failed.");
        }

        var history = await _repository.GetHistoryAsync(
            operatorId,
            normalizedResult,
            page,
            pageSize);

        _logger.LogInformation(
            "Inspection history queried. OperatorId={OperatorId}, Result={Result}, TotalCount={TotalCount}",
            operatorId,
            normalizedResult,
            history.TotalCount);

        return history;
    }


    public async Task<PendingInspectionPageDto> GetPendingAsync(
        bool? isDue,
        int page,
        int pageSize)
    {
        // 1. 验证页码
        if (page < 1)
        {
            throw new ValidationException(
                "Page must be greater than or equal to 1.");
        }

        // 2. 验证每页数量
        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException(
                "PageSize must be between 1 and 100.");
        }

        // 3. 验证分页偏移量是否超出 int 范围
        long offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            throw new ValidationException(
                "Pagination offset is too large.");
        }

        // 4. 调用 Repository
        PendingInspectionPageDto result =
            await _repository.GetPendingAsync(
                isDue,
                page,
                pageSize);

        // 5. 记录查询日志
        _logger.LogInformation(
            "Pending inspections queried. IsDue={IsDue}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            isDue,
            page,
            pageSize,
            result.TotalCount);

        return result;
    }



    public async Task<InspectionClaimResult> ClaimAsync(
        int inspectionId,
        int operatorId)
    {
        // 1. 检查任务 ID
        if (inspectionId <= 0)
        {
            throw new ValidationException(
                "Invalid inspection id.");
        }

        // 2. 检查业务员 ID
        if (operatorId <= 0)
        {
            throw new ValidationException(
                "Invalid operator id.");
        }

        // 3. 调用 Repository 执行领取事务
        InspectionClaimResult result =
            await _repository.ClaimAsync(
                inspectionId,
                operatorId);

        // 4. 根据业务结果记录日志
        switch (result)
        {
            case InspectionClaimResult.Success:
                _logger.LogInformation(
                    "Inspection {InspectionId} claimed by Operator {OperatorId}.",
                    inspectionId,
                    operatorId);
                break;

            case InspectionClaimResult.NotFound:
                _logger.LogInformation(
                    "Inspection {InspectionId} not found.",
                    inspectionId);
                break;

            case InspectionClaimResult.NotDue:
                _logger.LogWarning(
                    "Inspection {InspectionId} is not due yet.",
                    inspectionId);
                break;

            case InspectionClaimResult.AlreadyClaimed:
                _logger.LogInformation(
                    "Inspection {InspectionId} has already been claimed.",
                    inspectionId);
                break;

            case InspectionClaimResult.InvalidItemStatus:
                _logger.LogWarning(
                    "Inspection {InspectionId} has invalid rental item status.",
                    inspectionId);
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown inspection claim result.");
        }

        return result;
    }


    public async Task<InspectionCompleteResult> CompleteAsync(
        int inspectionId,
        int operatorId,
        bool isAdmin,
        string result,
        string? resultNotes)
    {
        if (inspectionId <= 0 || operatorId <= 0)
            throw new ValidationException(
                "Invalid inspection or operator id.");

        string normalizedResult = result?.Trim() ?? "";

        if (normalizedResult != "Passed" &&
            normalizedResult != "Failed")
        {
            throw new ValidationException(
                "Result must be Passed or Failed.");
        }

        string? notes = resultNotes?.Trim();

        if (notes?.Length > 500)
            throw new ValidationException(
                "Result notes exceed 500 characters.");

        if (normalizedResult == "Failed" &&
            string.IsNullOrWhiteSpace(notes))
        {
            throw new ValidationException(
                "Failure reason is required.");
        }

        InspectionCompleteResult outcome =
            await _repository.CompleteAsync(
                inspectionId,
                operatorId,
                isAdmin,
                normalizedResult,
                notes);

        _logger.LogInformation(
            "Inspection completion processed. Id={InspectionId}, Result={Result}, Outcome={Outcome}",
            inspectionId,
            normalizedResult,
            outcome);

        return outcome;
    }


}
