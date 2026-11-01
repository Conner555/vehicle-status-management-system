using VehicleStatusSystem.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Domain;

namespace VehicleStatusSystem.Repositories;

public class InspectionRepository : IInspectionRepository
{
    private readonly string _connectionString;

    public InspectionRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<PendingInspectionPageDto> GetPendingAsync(
        bool? isDue,
        int page,
        int pageSize)
    {
        int offset = checked((page - 1) * pageSize);

        const string sql = """
            DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

            SELECT COUNT(*)
            FROM dbo.VehicleInspections AS vi
            WHERE vi.Status = 'Pending'
              AND (
                  @IsDue IS NULL
                  OR (@IsDue = 1 AND vi.InspectionDueAt <= @Now)
                  OR (@IsDue = 0 AND vi.InspectionDueAt > @Now)
              );

            SELECT
                vi.Id AS InspectionId,
                vi.RentalOrderItemId AS OrderItemId,
                i.OrderId,
                vi.VehicleId,
                v.PlateNumber,
                vm.ModelName,
                vi.Status,
                vi.ReturnedAt,
                vi.InspectionDueAt,
                CASE
                    WHEN vi.InspectionDueAt <= @Now
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END AS IsDue
            FROM dbo.VehicleInspections AS vi
            INNER JOIN dbo.RentalOrderItems AS i
                ON i.Id = vi.RentalOrderItemId
            INNER JOIN dbo.RentalOrders AS o
                ON o.Id = i.OrderId
            INNER JOIN dbo.Vehicles AS v
                ON v.Id = vi.VehicleId
            INNER JOIN dbo.VehicleModels AS vm
                ON vm.Id = v.VehicleModelId
            WHERE vi.Status = 'Pending'
              AND (
                  @IsDue IS NULL
                  OR (@IsDue = 1 AND vi.InspectionDueAt <= @Now)
                  OR (@IsDue = 0 AND vi.InspectionDueAt > @Now)
              )
            ORDER BY vi.InspectionDueAt ASC, vi.Id ASC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@IsDue", SqlDbType.Bit
        ).Value = isDue.HasValue
            ? isDue.Value
            : DBNull.Value;

        command.Parameters.Add(
            "@Offset", SqlDbType.Int
        ).Value = offset;

        command.Parameters.Add(
            "@PageSize", SqlDbType.Int
        ).Value = pageSize;

        var result = new PendingInspectionPageDto
        {
            Page = page,
            PageSize = pageSize
        };

        await using var reader =
            await command.ExecuteReaderAsync();

        // 第一个结果集：总数量
        if (await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        // 切换到第二个结果集：分页任务
        if (!await reader.NextResultAsync())
        {
            throw new InvalidOperationException(
                "Inspection query result is missing.");
        }

        while (await reader.ReadAsync())
        {
            result.Items.Add(new PendingInspectionDto
            {
                InspectionId =
                    reader.GetInt32(0),

                OrderItemId =
                    reader.GetInt32(1),

                OrderId =
                    reader.GetInt32(2),

                VehicleId =
                    reader.GetInt32(3),

                PlateNumber =
                    reader.GetString(4),

                ModelName =
                    reader.GetString(5),

                Status =
                    reader.GetString(6),

                ReturnedAt =
                    reader.GetDateTime(7),

                InspectionDueAt =
                    reader.GetDateTime(8),

                IsDue =
                    reader.GetBoolean(9)
            });
        }

        return result;
    }

    public async Task<InspectionClaimResult> ClaimAsync(
        int inspectionId,
        int operatorId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 查询任务对应的车辆、订单和明细
        // 当前设计中这些关联一经创建不可修改
        const string findSql = """
            SELECT vi.VehicleId, i.OrderId,
                vi.RentalOrderItemId
            FROM dbo.VehicleInspections vi
            JOIN dbo.RentalOrderItems i
                ON i.Id = vi.RentalOrderItemId
            WHERE vi.Id = @InspectionId
            AND vi.VehicleId = i.VehicleId;
            """;

        int vehicleId;
        int orderId;
        int itemId;

        await using (var cmd =
            new SqlCommand(findSql, connection))
        {
            cmd.Parameters.Add(
                "@InspectionId", SqlDbType.Int
            ).Value = inspectionId;

            await using var reader =
                await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return InspectionClaimResult.NotFound;

            vehicleId = reader.GetInt32(0);
            orderId = reader.GetInt32(1);
            itemId = reader.GetInt32(2);
        }

        // 2. 开启数据库事务
        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 3. 先锁车辆
            const string lockVehicleSql = """
                SELECT Id
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            await using (var cmd =
                new SqlCommand(
                    lockVehicleSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int
                ).Value = vehicleId;

                if (await cmd.ExecuteScalarAsync() is null)
                    throw new InvalidOperationException(
                        "Vehicle not found.");
            }

            // 4. 锁订单
            const string lockOrderSql = """
                SELECT Id
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId;
                """;

            await using (var cmd =
                new SqlCommand(
                    lockOrderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int
                ).Value = orderId;

                if (await cmd.ExecuteScalarAsync() is null)
                    throw new InvalidOperationException(
                        "Order not found.");
            }

            // 5. 锁定车辆明细并检查归还状态
            const string itemSql = """
                SELECT Status
                FROM dbo.RentalOrderItems
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @ItemId
                AND OrderId = @OrderId
                AND VehicleId = @VehicleId;
                """;

            string? itemStatus;

            await using (var cmd =
                new SqlCommand(itemSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int
                ).Value = itemId;

                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int
                ).Value = orderId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int
                ).Value = vehicleId;

                itemStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (itemStatus != "PendingInspection")
            {
                await transaction.RollbackAsync();

                return InspectionClaimResult.InvalidItemStatus;
            }

            // 6. 锁定验车任务
            const string inspectionSql = """
                SELECT Status, InspectionDueAt
                FROM dbo.VehicleInspections
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @InspectionId
                AND RentalOrderItemId = @ItemId
                AND VehicleId = @VehicleId;
                """;

            string? status = null;
            DateTime dueAt = default;

            await using (var cmd =
                new SqlCommand(
                    inspectionSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@InspectionId", SqlDbType.Int
                ).Value = inspectionId;

                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int
                ).Value = itemId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int
                ).Value = vehicleId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    status = reader.GetString(0);
                    dueAt = reader.GetDateTime(1);
                }
            }

            if (status is null)
            {
                await transaction.RollbackAsync();
                return InspectionClaimResult.NotFound;
            }

            if (status != "Pending")
            {
                await transaction.RollbackAsync();
                return InspectionClaimResult.AlreadyClaimed;
            }

            // 7. 使用 SQL Server 当前 UTC 时间判断
            const string updateSql = """
                UPDATE dbo.VehicleInspections
                SET Status = 'InProgress',
                    OperatorId = @OperatorId,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @InspectionId
                AND RentalOrderItemId = @ItemId
                AND Status = 'Pending'
                AND OperatorId IS NULL;
                """;

            // dueAt 在前面读取，本次时间校验最终以
            // 条件 UPDATE 使用的数据库时间为准
            await using (var cmd =
                new SqlCommand(
                    updateSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@InspectionId", SqlDbType.Int
                ).Value = inspectionId;

                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int
                ).Value = itemId;

                cmd.Parameters.Add(
                    "@OperatorId", SqlDbType.Int
                ).Value = operatorId;

                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected != 1)
                {
                    await transaction.RollbackAsync();

                    return InspectionClaimResult.AlreadyClaimed;
                }
            }

            await transaction.CommitAsync();

            return InspectionClaimResult.Success;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (SqlException) { }
            catch (InvalidOperationException) { }

            throw;
        }
    }

    private static async Task CreateEarlyReturnRefundAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int orderId,
        int itemId)
    {
        // 1. 检查退款规则版本
        const string policySql = """
            SELECT RefundPolicyVersion
            FROM dbo.RentalOrders
            WHERE Id = @OrderId;
            """;

        int policyVersion;

        await using (var cmd = new SqlCommand(
            policySql, connection, transaction))
        {
            cmd.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            object? value = await cmd.ExecuteScalarAsync();

            if (value is null)
                throw new InvalidOperationException(
                    "Rental order not found.");

            policyVersion = Convert.ToInt32(value);
        }

        // 历史订单不自动补发退款
        if (policyVersion != 2)
            return;

        // 2. 读取结算依据
        const string itemSql = """
            SELECT
                RentalDays,
                DailyRate,
                Subtotal,
                ActualHandedOverAt,
                ActualReturnedAt
            FROM dbo.RentalOrderItems
            WHERE Id = @ItemId
            AND OrderId = @OrderId
            AND Status = 'Completed';
            """;

        int bookedDays;
        decimal dailyRate;
        decimal subtotal;
        DateTime handoverAt;
        DateTime returnedAt;

        await using (var cmd = new SqlCommand(
            itemSql, connection, transaction))
        {
            cmd.Parameters.Add(
                "@ItemId", SqlDbType.Int).Value = itemId;
            cmd.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            await using var reader =
                await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Completed rental item not found.");

            bookedDays = reader.GetInt32(0);
            dailyRate = reader.GetDecimal(1);
            subtotal = reader.GetDecimal(2);

            if (reader.IsDBNull(3) || reader.IsDBNull(4))
                throw new InvalidOperationException(
                    "Actual rental timestamps are missing.");

            handoverAt = reader.GetDateTime(3);
            returnedAt = reader.GetDateTime(4);
        }

        // 3. 调用现有 Domain 计算器
        var settlement = RentalSettlementCalculator.Calculate(
            handoverAt,
            returnedAt,
            bookedDays,
            dailyRate);

        if (settlement.OriginalAmount != subtotal)
            throw new InvalidOperationException(
                "Rental amount is inconsistent.");

        decimal refundAmount = settlement.RefundAmount;

        // 4. 无差额时，无需插入退款记录
        if (refundAmount == 0m)
            return;

        // 5. 检查是否已有提前归还退款
        const string existingSql = """
            SELECT Status
            FROM dbo.Refunds WITH (UPDLOCK, HOLDLOCK)
            WHERE OrderItemId = @ItemId
            AND Reason = 'EarlyReturn';
            """;

        await using (var cmd = new SqlCommand(
            existingSql, connection, transaction))
        {
            cmd.Parameters.Add(
                "@ItemId", SqlDbType.Int).Value = itemId;

            object? existing = await cmd.ExecuteScalarAsync();

            if (existing is not null)
                throw new InvalidOperationException(
                    "Early return refund already exists.");
        }

        // 6. 查找成功支付记录
        const string paymentSql = """
            SELECT
                Id,
                Amount
            FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
            WHERE OrderId = @OrderId
            AND Status = 'Succeeded';
            """;

        long paymentId;
        decimal paidAmount;

        await using (var cmd = new SqlCommand(
            paymentSql, connection, transaction))
        {
            cmd.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            await using var reader =
                await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Successful payment not found.");

            paymentId = reader.GetInt64(0);
            paidAmount = reader.GetDecimal(1);

            if (await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Multiple successful payments found.");
        }

        // 7. 检查已成功退款总额
        const string refundedSql = """
            SELECT COALESCE(SUM(Amount), 0)
            FROM dbo.Refunds WITH (UPDLOCK, HOLDLOCK)
            WHERE PaymentId = @PaymentId
            AND Status = 'Succeeded';
            """;

        decimal refundedAmount;

        await using (var cmd = new SqlCommand(
            refundedSql, connection, transaction))
        {
            cmd.Parameters.Add(
                "@PaymentId", SqlDbType.BigInt).Value = paymentId;

            refundedAmount = Convert.ToDecimal(
                await cmd.ExecuteScalarAsync());
        }

        if (refundAmount + refundedAmount > paidAmount)
            throw new InvalidOperationException(
                "Refund exceeds the original payment.");

        // 8. 写入模拟成功退款
        const string insertSql = """
            INSERT INTO dbo.Refunds
            (
                OrderId,
                OrderItemId,
                PaymentId,
                IdempotencyKey,
                Amount,
                Reason,
                Status,
                RefundedAt
            )
            VALUES
            (
                @OrderId,
                @ItemId,
                @PaymentId,
                @IdempotencyKey,
                @Amount,
                'EarlyReturn',
                'Succeeded',
                SYSUTCDATETIME()
            );
            """;

        await using (var cmd = new SqlCommand(
            insertSql, connection, transaction))
        {
            cmd.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            cmd.Parameters.Add(
                "@ItemId", SqlDbType.Int).Value = itemId;

            cmd.Parameters.Add(
                "@PaymentId", SqlDbType.BigInt).Value = paymentId;

            // 内部自动结算生成唯一标识
            cmd.Parameters.Add(
                "@IdempotencyKey",
                SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();

            var amountParam = cmd.Parameters.Add(
                "@Amount", SqlDbType.Decimal);

            amountParam.Precision = 18;
            amountParam.Scale = 2;
            amountParam.Value = refundAmount;

            if (await cmd.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException(
                    "Early return refund insert failed.");
        }
    }

    public async Task<InspectionCompleteResult> CompleteAsync(
        int inspectionId,
        int operatorId,
        bool isAdmin,
        string result,
        string? resultNotes)
    {
        //检测result状态，防止operator填入的其他结果干扰判断
        if (result is not ("Passed" or "Failed"))
            throw new ArgumentException(
                "Invalid inspection result.", nameof(result));

        if (result == "Failed" &&
            string.IsNullOrWhiteSpace(resultNotes))
            throw new ArgumentException(
                "Failure reason is required.", nameof(resultNotes));

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 读取验车任务关联的业务 ID
        const string findSql = """
            SELECT
                vi.VehicleId,
                i.OrderId,
                vi.RentalOrderItemId
            FROM dbo.VehicleInspections vi
            JOIN dbo.RentalOrderItems i
            ON i.Id = vi.RentalOrderItemId
            AND i.VehicleId = vi.VehicleId
            WHERE vi.Id = @InspectionId;
            """;

        int vehicleId, orderId, itemId;

        await using (var cmd = new SqlCommand(findSql, connection))
        {
            cmd.Parameters.Add("@InspectionId", SqlDbType.Int)
                .Value = inspectionId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return InspectionCompleteResult.NotFound;

            vehicleId = reader.GetInt32(0);
            orderId = reader.GetInt32(1);
            itemId = reader.GetInt32(2);
        }

        // 2. 事务开始
        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        async Task<InspectionCompleteResult> AbortAsync(
            InspectionCompleteResult reason)
        {
            await transaction.RollbackAsync();
            return reason;
        }

        try
        {
            // 3. 锁车辆
            const string vehicleSql = """
                SELECT Id
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            await using (var cmd =
                new SqlCommand(vehicleSql, connection, transaction))
            {
                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                if (await cmd.ExecuteScalarAsync() is null)
                    return await AbortAsync(
                        InspectionCompleteResult.NotFound);
            }

            // 4. 锁订单并校验订单状态
            const string orderSql = """
                SELECT Status
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId;
                """;

            string? orderStatus;

            await using (var cmd =
                new SqlCommand(orderSql, connection, transaction))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int)
                    .Value = orderId;

                orderStatus = (string?)await cmd.ExecuteScalarAsync();
            }

            if (orderStatus != "InUse")
                return await AbortAsync(
                    InspectionCompleteResult.InvalidOrderStatus);

            // 5. 锁租赁明细并验证归还状态
            const string itemSql = """
                SELECT Status
                FROM dbo.RentalOrderItems WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @ItemId
                AND OrderId = @OrderId
                AND VehicleId = @VehicleId;
                """;

            string? itemStatus;

            await using (var cmd =
                new SqlCommand(itemSql, connection, transaction))
            {
                cmd.Parameters.Add("@ItemId", SqlDbType.Int)
                    .Value = itemId;
                cmd.Parameters.Add("@OrderId", SqlDbType.Int)
                    .Value = orderId;
                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                itemStatus = (string?)await cmd.ExecuteScalarAsync();
            }

            // 6. 锁验车任务，检查领取人与状态
            const string inspectionSql = """
                SELECT Status, OperatorId
                FROM dbo.VehicleInspections WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @InspectionId
                AND RentalOrderItemId = @ItemId
                AND VehicleId = @VehicleId;
                """;

            string? inspectionStatus = null;
            int? assignedOperatorId = null;

            await using (var cmd =
                new SqlCommand(inspectionSql, connection, transaction))
            {
                cmd.Parameters.Add("@InspectionId", SqlDbType.Int)
                    .Value = inspectionId;
                cmd.Parameters.Add("@ItemId", SqlDbType.Int)
                    .Value = itemId;
                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    inspectionStatus = reader.GetString(0);
                    assignedOperatorId = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1);
                }
            }

            if (inspectionStatus is null)
                return await AbortAsync(
                    InspectionCompleteResult.NotFound);

            if (inspectionStatus is "Passed" or "Failed")
                return await AbortAsync(
                    InspectionCompleteResult.AlreadyCompleted);

            if (inspectionStatus != "InProgress")
                return await AbortAsync(
                    InspectionCompleteResult.NotClaimed);

            if (!isAdmin && assignedOperatorId != operatorId)
                return await AbortAsync(
                    InspectionCompleteResult.Forbidden);

            if (itemStatus != "PendingInspection")
                return await AbortAsync(
                    InspectionCompleteResult.InvalidItemStatus);

            // 7. 更新验车任务
            const string updateInspectionSql = """
                UPDATE dbo.VehicleInspections
                SET Status = @Result,
                    ResultNotes = @ResultNotes,
                    InspectedAt = SYSUTCDATETIME(),
                    OperatorId = @OperatorId,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @InspectionId
                AND Status = 'InProgress';
                """;

            await using (var cmd = new SqlCommand(
                updateInspectionSql, connection, transaction))
            {
                cmd.Parameters.Add("@Result", SqlDbType.NVarChar, 20)
                    .Value = result;
                cmd.Parameters.Add("@ResultNotes", SqlDbType.NVarChar, 500)
                    .Value = (object?)resultNotes ?? DBNull.Value;
                cmd.Parameters.Add("@OperatorId", SqlDbType.Int)
                    .Value = operatorId;
                cmd.Parameters.Add("@InspectionId", SqlDbType.Int)
                    .Value = inspectionId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Inspection update failed.");
            }

            // 8. 完成该车辆的租赁明细
            const string updateItemSql = """
                UPDATE dbo.RentalOrderItems
                SET Status = 'Completed',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @ItemId
                AND OrderId = @OrderId
                AND Status = 'PendingInspection';
                """;

            await using (var cmd = new SqlCommand(
                updateItemSql, connection, transaction))
            {
                cmd.Parameters.Add("@ItemId", SqlDbType.Int)
                    .Value = itemId;
                cmd.Parameters.Add("@OrderId", SqlDbType.Int)
                    .Value = orderId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Rental item update failed.");
            }

            // 9. 根据验车结果更新车辆运营状态
            const string updateVehicleSql = """
                UPDATE dbo.Vehicles
                SET OperationStatus = @OperationStatus
                WHERE Id = @VehicleId;
                """;

            string operationStatus =
                result == "Passed" ? "Available" : "Maintenance";

            await using (var cmd = new SqlCommand(
                updateVehicleSql, connection, transaction))
            {
                cmd.Parameters.Add("@OperationStatus",
                    SqlDbType.NVarChar, 30).Value = operationStatus;

                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Vehicle status update failed.");
            }

            // 10、仅在验车失败时自动创建维修任务（这里如果结果是failed进入失败分支）
            if (result == "Failed")
            {
                const string insertMaintenanceSql = """
                    INSERT INTO dbo.VehicleMaintenances
                    (
                        VehicleId,
                        InspectionId,
                        ReportedBy,
                        IssueDescription,
                        Status,
                        Priority
                    )
                    VALUES
                    (
                        @VehicleId,
                        @InspectionId,
                        @OperatorId,
                        @IssueDescription,
                        'Pending',
                        'Medium'
                    );
                    """;

                await using var cmd = new SqlCommand(
                    insertMaintenanceSql,
                    connection,
                    transaction);

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                cmd.Parameters.Add(
                    "@InspectionId", SqlDbType.Int).Value = inspectionId;

                cmd.Parameters.Add(
                    "@OperatorId", SqlDbType.Int).Value = operatorId;

                cmd.Parameters.Add(
                    "@IssueDescription", SqlDbType.NVarChar, 500)
                    .Value = resultNotes!;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        "Maintenance task creation failed.");
                }
            }

            // V3.4.3：验车通过后自动执行提前归还差额退款
            if (result == "Passed")
            {
                await CreateEarlyReturnRefundAsync(
                    connection,
                    transaction,
                    orderId,
                    itemId);
            }


            // 11. 如果所有明细已完成，关闭整笔订单
            const string completeOrderSql = """
                UPDATE dbo.RentalOrders
                SET Status = 'Completed',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OrderId
                AND Status = 'InUse'
                AND EXISTS (
                    SELECT 1 FROM dbo.RentalOrderItems
                    WHERE OrderId = @OrderId
                )
                AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.RentalOrderItems
                    WHERE OrderId = @OrderId
                        AND Status <> 'Completed'
                );
                """;

            await using (var cmd = new SqlCommand(
                completeOrderSql, connection, transaction))
            {
                cmd.Parameters.Add("@OrderId", SqlDbType.Int)
                    .Value = orderId;

                // 如果还有其他车辆未完成，这里更新 0 行是正常的
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return InspectionCompleteResult.Success;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (SqlException) { }
            catch (InvalidOperationException) { }

            throw;
        }
    }


}
