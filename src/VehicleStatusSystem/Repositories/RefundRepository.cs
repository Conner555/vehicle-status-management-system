
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly string _connectionString;

    public RefundRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<(CancelPaidOrderStatus Status,
        CancelPaidOrderResultDto? Refund)> CancelPaidOrderAsync(
        int orderId,
        int customerId,
        Guid idempotencyKey)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 与现有订单模块保持一致：
        // 先获取车辆 ID，之后统一按升序加锁
        var vehicleIds = new List<int>();

        const string vehicleIdsSql = """
            SELECT VehicleId
            FROM dbo.RentalOrderItems
            WHERE OrderId = @OrderId
            ORDER BY VehicleId;
            """;

        await using (var cmd =
            new SqlCommand(vehicleIdsSql, connection))
        {
            cmd.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                vehicleIds.Add(reader.GetInt32(0));
        }

        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(
                    IsolationLevel.ReadCommitted);

        try
        {
            // 1. 统一顺序锁定车辆
            foreach (int vehicleId in
                vehicleIds.Distinct().OrderBy(x => x))
            {
                const string lockSql = """
                    SELECT Id
                    FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                    WHERE Id = @VehicleId;
                    """;

                await using var cmd =
                    new SqlCommand(
                        lockSql, connection, transaction);

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value =
                    vehicleId;

                if (await cmd.ExecuteScalarAsync() is null)
                    throw new InvalidOperationException(
                        "Referenced vehicle not found.");
            }

            // 2. 锁定订单并验证归属
            string? orderStatus = null;
            decimal orderAmount = 0m;

            const string orderSql = """
                SELECT Status, TotalAmount
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId
                  AND CustomerId = @CustomerId;
                """;

            await using (var cmd =
                new SqlCommand(orderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value = customerId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    orderStatus = reader.GetString(0);
                    orderAmount = reader.GetDecimal(1);
                }
            }

            if (orderStatus is null)
            {
                await transaction.RollbackAsync();
                return (CancelPaidOrderStatus.NotFound, null);
            }

            // 3. 检查已有退款，用于幂等重试
            const string existingSql = """
                SELECT
                    Id, RefundNo, Amount, Status,
                    IdempotencyKey, Reason
                FROM dbo.Refunds WITH (UPDLOCK, HOLDLOCK)
                WHERE OrderId = @OrderId
                  AND Reason = 'Cancellation';
                """;

            await using (var cmd =
                new SqlCommand(existingSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    long refundId = reader.GetInt64(0);
                    Guid refundNo = reader.GetGuid(1);
                    decimal amount = reader.GetDecimal(2);
                    string status = reader.GetString(3);
                    Guid existingKey = reader.GetGuid(4);

                    await reader.CloseAsync();
                    await transaction.RollbackAsync();

                    if (existingKey != idempotencyKey)
                        return (
                            CancelPaidOrderStatus.AlreadyRefunded,
                            null);

                    if (status != "Succeeded")
                        return (
                            CancelPaidOrderStatus.IdempotencyConflict,
                            null);

                    return (
                        CancelPaidOrderStatus.Success,
                        new CancelPaidOrderResultDto
                        {
                            OrderId = orderId,
                            RefundId = refundId,
                            RefundNo = refundNo,
                            RefundAmount = amount,
                            RefundStatus = status,
                            OrderStatus = "Cancelled"
                        });
                }
            }

            if (orderStatus != "Confirmed")
            {
                await transaction.RollbackAsync();
                return (CancelPaidOrderStatus.InvalidStatus, null);
            }

            // 4. 验证全部订单明细仍未交车
            const string itemsSql = """
                SELECT
                    COUNT(*) AS TotalCount,
                    COALESCE(SUM(CASE
                        WHEN Status = 'Reserved'
                         AND ActualHandedOverAt IS NULL
                        THEN 1 ELSE 0 END), 0) AS ReservedCount,
                    COALESCE(SUM(Subtotal), 0) AS TotalAmount
                FROM dbo.RentalOrderItems
                WHERE OrderId = @OrderId;
                """;

            int totalCount;
            int reservedCount;
            decimal itemsAmount;

            await using (var cmd =
                new SqlCommand(itemsSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                await reader.ReadAsync();

                totalCount = reader.GetInt32(0);
                reservedCount = reader.GetInt32(1);
                itemsAmount = reader.GetDecimal(2);
            }

            if (totalCount == 0 ||
                totalCount != reservedCount ||
                totalCount != vehicleIds.Count)
            {
                await transaction.RollbackAsync();
                return (
                    CancelPaidOrderStatus.AlreadyHandedOver,
                    null);
            }

            if (itemsAmount != orderAmount || orderAmount <= 0)
                throw new InvalidOperationException(
                    "Order amount is inconsistent.");

            // 5. 查找成功支付记录
            long paymentId;
            decimal paidAmount;

            const string paymentSql = """
                SELECT Id, Amount
                FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
                WHERE OrderId = @OrderId
                  AND Status = 'Succeeded';
                """;

            await using (var cmd =
                new SqlCommand(paymentSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    await reader.CloseAsync();
                    await transaction.RollbackAsync();

                    return (
                        CancelPaidOrderStatus.PaymentNotFound,
                        null);
                }

                paymentId = reader.GetInt64(0);
                paidAmount = reader.GetDecimal(1);
            }

            if (paidAmount != orderAmount)
                throw new InvalidOperationException(
                    "Payment amount is inconsistent.");

            // 6. 避免复用其他退款业务的 Key
            const string keySql = """
                SELECT COUNT(*)
                FROM dbo.Refunds WITH (UPDLOCK, HOLDLOCK)
                WHERE IdempotencyKey = @Key;
                """;

            await using (var cmd =
                new SqlCommand(keySql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@Key", SqlDbType.UniqueIdentifier).Value =
                    idempotencyKey;

                int count = Convert.ToInt32(
                    await cmd.ExecuteScalarAsync());

                if (count > 0)
                {
                    await transaction.RollbackAsync();
                    return (
                        CancelPaidOrderStatus.IdempotencyConflict,
                        null);
                }
            }

            // 7. 创建模拟成功退款记录
            const string insertSql = """
                INSERT INTO dbo.Refunds
                    (OrderId, OrderItemId, PaymentId,
                     IdempotencyKey, Amount, Reason,
                     Status, RefundedAt)
                OUTPUT INSERTED.Id, INSERTED.RefundNo
                VALUES
                    (@OrderId, NULL, @PaymentId,
                     @Key, @Amount, 'Cancellation',
                     'Succeeded', SYSUTCDATETIME());
                """;

            long newRefundId;
            Guid newRefundNo;

            await using (var cmd =
                new SqlCommand(insertSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add(
                    "@PaymentId", SqlDbType.BigInt).Value = paymentId;
                cmd.Parameters.Add(
                    "@Key", SqlDbType.UniqueIdentifier).Value =
                    idempotencyKey;

                var amountParam = cmd.Parameters.Add(
                    "@Amount", SqlDbType.Decimal);
                amountParam.Precision = 18;
                amountParam.Scale = 2;
                amountParam.Value = paidAmount;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    throw new DataException(
                        "Refund insert returned no data.");

                newRefundId = reader.GetInt64(0);
                newRefundNo = reader.GetGuid(1);
            }

            // 8. 取消订单明细
            const string cancelItemsSql = """
                UPDATE dbo.RentalOrderItems
                SET Status = 'Cancelled',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE OrderId = @OrderId
                  AND Status = 'Reserved'
                  AND ActualHandedOverAt IS NULL;
                """;

            await using (var cmd =
                new SqlCommand(
                    cancelItemsSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                if (await cmd.ExecuteNonQueryAsync() != totalCount)
                    throw new InvalidOperationException(
                        "Order items changed unexpectedly.");
            }

            // 9. 取消订单主表
            const string cancelOrderSql = """
                UPDATE dbo.RentalOrders
                SET Status = 'Cancelled',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OrderId
                  AND CustomerId = @CustomerId
                  AND Status = 'Confirmed';
                """;

            await using (var cmd =
                new SqlCommand(
                    cancelOrderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value = customerId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Order status changed unexpectedly.");
            }

            await transaction.CommitAsync();

            return (
                CancelPaidOrderStatus.Success,
                new CancelPaidOrderResultDto
                {
                    OrderId = orderId,
                    RefundId = newRefundId,
                    RefundNo = newRefundNo,
                    RefundAmount = paidAmount,
                    RefundStatus = "Succeeded",
                    OrderStatus = "Cancelled"
                });
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // 事务可能已经结束
            }
            catch (SqlException)
            {
                // 保留原始异常
            }

            throw;
        }
    }
}
