
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;

    public PaymentRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }

    public async Task<PaymentResult> PayAsync(
        int orderId,
        int customerId,
        string method,
        Guid idempotencyKey)
    {

        if(idempotencyKey == Guid.Empty)
        {
            throw new ArgumentException(
                "Idempotency key cannot be empty.",
                nameof(idempotencyKey));
        }

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 读取关联车辆 ID。
        // 当前阶段订单创建后不允许修改明细集合。
        const string vehicleIdsSql = """
            SELECT VehicleId
            FROM dbo.RentalOrderItems
            WHERE OrderId = @OrderId
            ORDER BY VehicleId;
            """;

        var vehicleIds = new List<int>();

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

        // 2. 开启事务
        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 3. 与取消、过期操作保持车辆锁顺序一致
            const string lockVehicleSql = """
                SELECT Id
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            foreach (int vehicleId in vehicleIds.Distinct().OrderBy(x => x))
            {
                await using var cmd =
                    new SqlCommand(
                        lockVehicleSql, connection, transaction);

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                if (await cmd.ExecuteScalarAsync() is null)
                    throw new InvalidOperationException(
                        "Order references a missing vehicle.");
            }

            // 4. 锁定订单，检查归属、状态、到期时间
            const string orderSql = """
                SELECT Status, ExpiresAt, TotalAmount
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId
                  AND CustomerId = @CustomerId;
                """;

            string? orderStatus = null;
            DateTime? expiresAt = null;
            decimal amount = 0m;

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
                    expiresAt = reader.IsDBNull(1)
                        ? null
                        : reader.GetDateTime(1);
                    amount = reader.GetDecimal(2);
                }
            }

            if (orderStatus is null)
                return await AbortAsync(
                    transaction, PaymentStatus.NotFound);

            // 先确认订单属于当前用户，再查询幂等键
            PaymentResult? previous =
                await FindPreviousPaymentAsync(
                    connection,
                    transaction,
                    orderId,
                    method,
                    idempotencyKey,
                    orderStatus);

            if (previous is not null)
            {
                // 本次没有新写入，结束查询事务
                await transaction.RollbackAsync();

                return previous;
            }


            if (orderStatus == "Confirmed" ||
                orderStatus == "InUse" ||
                orderStatus == "Completed")
                return await AbortAsync(
                    transaction, PaymentStatus.AlreadyPaid);

            if (orderStatus != "PendingPayment")
                return await AbortAsync(
                    transaction, PaymentStatus.InvalidOrderStatus);

            // 使用数据库时间检查，避免应用服务器时间偏差
            const string timeSql = "SELECT SYSUTCDATETIME();";

            DateTime databaseNow;

            await using (var cmd =
                new SqlCommand(timeSql, connection, transaction))
            {
                databaseNow = Convert.ToDateTime(
                    await cmd.ExecuteScalarAsync());
            }

            if (!expiresAt.HasValue || expiresAt.Value <= databaseNow)
                return await AbortAsync(
                    transaction, PaymentStatus.Expired);

            // 5. 验证订单明细及金额一致性
            const string checkItemsSql = """
                SELECT
                    COUNT(*) AS TotalCount,
                    COALESCE(SUM(CASE
                        WHEN Status = 'Reserved'
                        THEN 1 ELSE 0 END), 0) AS ReservedCount,
                    COALESCE(SUM(Subtotal), 0) AS ItemsAmount
                FROM dbo.RentalOrderItems
                WHERE OrderId = @OrderId;
                """;

            int totalCount;
            int reservedCount;
            decimal itemsAmount;

            await using (var cmd =
                new SqlCommand(checkItemsSql, connection, transaction))
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
                totalCount != vehicleIds.Count ||
                amount <= 0 ||
                itemsAmount != amount)
            {
                throw new InvalidOperationException(
                    "Order items or amount are inconsistent.");
            }

            // 6. 插入模拟成功支付记录
            const string insertPaymentSql = """
                INSERT INTO dbo.Payments
                    (OrderId, IdempotencyKey, Amount, Method, Status, PaidAt)
                OUTPUT INSERTED.PaymentNo
                VALUES
                    (@OrderId, @IdempotencyKey, @Amount, @Method,
                     'Succeeded', SYSUTCDATETIME());
                """;

            Guid paymentNo;

            await using (var cmd =
                new SqlCommand(insertPaymentSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;
                    
                cmd.Parameters.Add(
                    "@IdempotencyKey",
                    SqlDbType.UniqueIdentifier).Value = idempotencyKey;

                var amountParam = cmd.Parameters.Add(
                    "@Amount", SqlDbType.Decimal);
                amountParam.Precision = 12;
                amountParam.Scale = 2;
                amountParam.Value = amount;

                cmd.Parameters.Add(
                    "@Method", SqlDbType.NVarChar, 30).Value = method;

                paymentNo = (Guid)(await cmd.ExecuteScalarAsync()
                    ?? throw new InvalidOperationException(
                        "Payment number was not returned."));
            }

            // 7. 更新订单状态，必须再次检查是否到期
            const string updateOrderSql = """
                UPDATE dbo.RentalOrders
                SET Status = 'Confirmed',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OrderId
                  AND CustomerId = @CustomerId
                  AND Status = 'PendingPayment'
                  AND ExpiresAt > SYSUTCDATETIME();
                """;

            await using (var cmd =
                new SqlCommand(updateOrderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value = customerId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    return await AbortAsync(
                        transaction, PaymentStatus.Expired);
            }

            // 8. 两张表都成功才提交
            await transaction.CommitAsync();

            return new PaymentResult(
                PaymentStatus.Succeeded,
                paymentNo,
                amount);
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

    private static async Task<PaymentResult> AbortAsync(
        SqlTransaction transaction,
        PaymentStatus status)
    {
        await transaction.RollbackAsync();
        return new PaymentResult(status);
    }


    //这里是重复支付的验证函数
    private static async Task<PaymentResult?> FindPreviousPaymentAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int orderId,
        string method,
        Guid idempotencyKey,
        string currentOrderStatus)
    {
        const string sql = """
            SELECT
                OrderId,
                Method,
                Status,
                PaymentNo,
                Amount
            FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
            WHERE IdempotencyKey = @IdempotencyKey;
            """;

        await using var command =
            new SqlCommand(sql, connection, transaction);

        command.Parameters.Add(
            "@IdempotencyKey", SqlDbType.UniqueIdentifier
        ).Value = idempotencyKey;

        await using var reader =
            await command.ExecuteReaderAsync();

        // 不存在对应 Key：允许继续尝试新支付
        if (!await reader.ReadAsync())
            return null;

        int previousOrderId = reader.GetInt32(0);
        string previousMethod = reader.GetString(1);
        string previousStatus = reader.GetString(2);
        Guid previousPaymentNo = reader.GetGuid(3);
        decimal previousAmount = reader.GetDecimal(4);

        // 相同 Key 不允许用于不同订单或支付方式
        if (previousOrderId != orderId ||
            previousMethod != method)
        {
            return new PaymentResult(
                PaymentStatus.IdempotencyConflict);
        }

        // 当前模块只重放已成功提交的模拟支付
        if (previousStatus != "Succeeded")
        {
            return new PaymentResult(
                PaymentStatus.IdempotencyConflict);
        }

        // 发现支付成功记录，但订单状态不一致
        if (currentOrderStatus != "Confirmed")
        {
            throw new InvalidOperationException(
                "Payment and order status are inconsistent.");
        }

        // 使用原来的支付流水号和金额
        return new PaymentResult(
            PaymentStatus.IdempotentReplay,
            previousPaymentNo,
            previousAmount);
    }

}
