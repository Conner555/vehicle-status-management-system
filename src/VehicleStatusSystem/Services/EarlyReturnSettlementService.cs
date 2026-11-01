
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.Domain;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Services;

public class EarlyReturnSettlementService
    : IEarlyReturnSettlementService
{
    private readonly IEarlyReturnSettlementRepository _repository;

    public EarlyReturnSettlementService(
        IEarlyReturnSettlementRepository repository)
    {
        _repository = repository;
    }

    public async Task<EarlyReturnSettlementDto?> GetPreviewAsync(
        int orderItemId,
        int customerId)
    {
        // 1. 验证请求参数
        if (orderItemId <= 0 || customerId <= 0)
        {
            throw new ValidationException(
                "Invalid order item or customer id.");
        }

        // 2. Repository 查询数据库原始信息
        var data = await _repository.GetPreviewDataAsync(
            orderItemId,
            customerId);

        if (data is null)
            return null;

        // 3. 构建基础响应
        var result = new EarlyReturnSettlementDto
        {
            OrderId = data.OrderId,
            OrderItemId = data.OrderItemId,
            BookedDays = data.BookedDays,
            OriginalAmount = data.Subtotal,
            InspectionStatus = data.InspectionStatus
        };

        // 4. 尚未交车或归还，不能计算实际租期
        if (!data.ActualHandedOverAt.HasValue ||
            !data.ActualReturnedAt.HasValue)
        {
            result.SettlementStatus = "NotReturned";
            return result;
        }

        // 5. 调用 Domain Calculator 计算租金
        var settlement = RentalSettlementCalculator.Calculate(
            data.ActualHandedOverAt.Value,
            data.ActualReturnedAt.Value,
            data.BookedDays,
            data.DailyRate);

        // 6. 检查金额一致性
        if (settlement.OriginalAmount != data.Subtotal)
        {
            throw new InvalidOperationException(
                "Rental subtotal is inconsistent.");
        }

        // 7. 写入计算结果
        result.ChargedDays = settlement.ChargedDays;
        result.FinalAmount = settlement.FinalAmount;
        result.RefundAmount = settlement.RefundAmount;

        // 8. 判断结算状态
        result.SettlementStatus = data.RefundStatus switch
        {
            "Succeeded" => "Refunded",

            "Pending" => "PendingRefund",

            "Failed" => "RefundFailed",

            _ when data.InspectionStatus == "Passed"
                && result.RefundAmount > 0
                => "ReadyForRefund",

            _ when data.InspectionStatus == "Passed"
                => "NoRefund",

            _ when data.InspectionStatus == "Failed"
                => "PendingReview",

            _ => "PendingInspection"
        };

        return result;
    }
}
