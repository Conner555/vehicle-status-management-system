
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Exceptions;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Repositories;

public class RentalOrderRepository : IRentalOrderRepository
{
    private readonly string _connectionString;

    public RentalOrderRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }

    public async Task<int> CreateAsync(
        int customerId,
        IReadOnlyList<RentalOrderLine> lines)
    {

        if (lines.Count == 0)
            throw new ArgumentException(
                "Order lines cannot be empty.");

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. 依次锁定车辆并检查可用性
            foreach (var line in lines.OrderBy(x => x.VehicleId))
            {
                await CheckVehicleAsync(
                    connection, transaction, line);
            }

            var pricedLines = new List<(
                RentalOrderLine Line,
                decimal DailyRate,
                decimal Subtotal
            )>();
            //在事务内生成价格的订单明细

            decimal totalAmount = 0m;

            foreach (var line in lines.OrderBy(x => x.VehicleId))
            {
                decimal dailyRate = await GetDailyRateAsync(
                    connection,
                    transaction,
                    line.VehicleId);

                decimal subtotal = checked(
                    dailyRate * line.RentalDays);

                totalAmount = checked(
                    totalAmount + subtotal);

                pricedLines.Add((
                    line,
                    dailyRate,
                    subtotal
                ));
            }

            // 2. 创建订单主表
            const string orderSql = """
                INSERT INTO dbo.RentalOrders
                (
                    CustomerId,
                    TotalAmount,
                    Status,
                    ExpiresAt
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @CustomerId,
                    @TotalAmount,
                    'PendingPayment',
                    DATEADD(MINUTE, 15, SYSUTCDATETIME())
                );
                """;

            int orderId;

            await using (var command =
                new SqlCommand(orderSql, connection, transaction))
            {
                command.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value =
                    customerId;

                command.Parameters.Add(
                    "@TotalAmount", SqlDbType.Decimal).Value =
                    totalAmount;

                command.Parameters["@TotalAmount"].Precision = 12;
                command.Parameters["@TotalAmount"].Scale = 2;

                orderId = Convert.ToInt32(
                    await command.ExecuteScalarAsync());
            }

            // 3. 创建全部车辆明细
            const string itemSql = """
                INSERT INTO dbo.RentalOrderItems
                (
                    OrderId, VehicleId,
                    StartAt, EndAt,
                    DailyRate, RentalDays,
                    Subtotal, Status
                )
                VALUES
                (
                    @OrderId, @VehicleId,
                    @StartAt, @EndAt,
                    @DailyRate, @RentalDays,
                    @Subtotal, 'Reserved'
                );
                """;

                
            foreach (var priced in pricedLines)
            {
                var line = priced.Line;

                await using var command =
                    new SqlCommand(itemSql, connection, transaction);

                command.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                command.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = line.VehicleId;

                command.Parameters.Add(
                    "@StartAt", SqlDbType.DateTime2).Value = line.StartAt;

                command.Parameters.Add(
                    "@EndAt", SqlDbType.DateTime2).Value = line.EndAt;

                var rate = command.Parameters.Add(
                    "@DailyRate", SqlDbType.Decimal);

                rate.Precision = 10;
                rate.Scale = 2;
                rate.Value = priced.DailyRate;

                command.Parameters.Add(
                    "@RentalDays", SqlDbType.Int).Value = line.RentalDays;

                var subtotal = command.Parameters.Add(
                    "@Subtotal", SqlDbType.Decimal);

                subtotal.Precision = 12;
                subtotal.Scale = 2;
                subtotal.Value = priced.Subtotal;

                await command.ExecuteNonQueryAsync();
            }

            // 4. 所有 SQL 成功，提交事务
            await transaction.CommitAsync();

            return orderId;
        }
        catch
        {
            // 5. 出现异常，回滚当前事务
            try
            {
                await transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // 事务可能已经被 SQL Server 终止
            }
            catch (SqlException)
            {
                // 回滚也可能因为连接失效而失败
            }

            throw;
        }
    }


    public async Task<RentalOrderDetailDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                o.Id AS OrderId,
                o.CustomerId,
                o.TotalAmount,
                o.Status AS OrderStatus,
                o.CreatedAt,

                i.Id AS ItemId,
                i.VehicleId,
                i.StartAt,
                i.EndAt,
                i.DailyRate,
                i.RentalDays,
                i.Subtotal,
                i.Status AS ItemStatus

            FROM dbo.RentalOrders o
            LEFT JOIN dbo.RentalOrderItems i
                ON o.Id = i.OrderId

            WHERE o.Id = @OrderId
            ORDER BY i.Id;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@OrderId", SqlDbType.Int).Value = id;

        await using var reader =
            await command.ExecuteReaderAsync();

        RentalOrderDetailDto? order = null;

        while (await reader.ReadAsync())
        {
            // 同一订单只有一个主表对象
            if (order is null)
            {
                order = new RentalOrderDetailDto
                {
                    Id = reader.GetInt32(
                        reader.GetOrdinal("OrderId")),

                    CustomerId = reader.GetInt32(
                        reader.GetOrdinal("CustomerId")),

                    TotalAmount = reader.GetDecimal(
                        reader.GetOrdinal("TotalAmount")),

                    Status = reader.GetString(
                        reader.GetOrdinal("OrderStatus")),

                    CreatedAt = reader.GetDateTime(
                        reader.GetOrdinal("CreatedAt"))
                };
            }

            int itemIdIndex = reader.GetOrdinal("ItemId");

            if (!reader.IsDBNull(itemIdIndex))
            {
                order.Items.Add(new RentalOrderItemDetailDto
                {
                    Id = reader.GetInt32(itemIdIndex),

                    VehicleId = reader.GetInt32(
                        reader.GetOrdinal("VehicleId")),

                    StartAt = reader.GetDateTime(
                        reader.GetOrdinal("StartAt")),

                    EndAt = reader.GetDateTime(
                        reader.GetOrdinal("EndAt")),

                    DailyRate = reader.GetDecimal(
                        reader.GetOrdinal("DailyRate")),

                    RentalDays = reader.GetInt32(
                        reader.GetOrdinal("RentalDays")),

                    Subtotal = reader.GetDecimal(
                        reader.GetOrdinal("Subtotal")),

                    Status = reader.GetString(
                        reader.GetOrdinal("ItemStatus"))
                });
            }
        }

        return order;
    }




    public async Task<CancelOrderResult> CancelAsync(
        int orderId,
        int customerId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 第一步：读取订单关联车辆 ID
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

        // 第二步：开启事务
        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 第三步：与创建订单采用相同的车辆锁顺序
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

            // 第四步：检查订单归属及状态，同时锁定订单
            const string orderSql = """
                SELECT Status
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId
                AND CustomerId = @CustomerId;
                """;

            string? status;

            await using (var cmd =
                new SqlCommand(orderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value = customerId;

                status = (string?)await cmd.ExecuteScalarAsync();
            }

            if (status is null)
            {
                await transaction.RollbackAsync();
                return CancelOrderResult.NotFound;
            }

            if (status != "PendingPayment")
            {
                await transaction.RollbackAsync();
                return CancelOrderResult.InvalidStatus;
            }

            // 第五步：检查明细状态是否全部允许取消
            const string countSql = """
                SELECT
                    COUNT(*) AS TotalCount,
                    COALESCE(SUM(
                        CASE WHEN Status = 'Reserved'
                            THEN 1 ELSE 0 END
                    ), 0) AS ReservedCount
                FROM dbo.RentalOrderItems
                WHERE OrderId = @OrderId;
                """;

            int totalCount;
            int reservedCount;

            await using (var cmd =
                new SqlCommand(countSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                await reader.ReadAsync();

                totalCount = reader.GetInt32(0);
                reservedCount = reader.GetInt32(1);
            }

            if (totalCount == 0 ||
                totalCount != reservedCount ||
                totalCount != vehicleIds.Count)
            {
                throw new InvalidOperationException(
                    "Order items are inconsistent.");
            }

            // 第六步：取消全部订单明细
            const string updateItemsSql = """
                UPDATE dbo.RentalOrderItems
                SET Status = 'Cancelled',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE OrderId = @OrderId
                AND Status = 'Reserved';
                """;

            await using (var cmd =
                new SqlCommand(
                    updateItemsSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected != totalCount)
                    throw new InvalidOperationException(
                        "Unexpected number of updated order items.");
            }

            // 第七步：取消订单主表
            const string updateOrderSql = """
                UPDATE dbo.RentalOrders
                SET Status = 'Cancelled',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OrderId
                AND CustomerId = @CustomerId
                AND Status = 'PendingPayment';
                """;

            await using (var cmd =
                new SqlCommand(
                    updateOrderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value = customerId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Order status changed unexpectedly.");
            }

            // 第八步：全部操作成功才提交
            await transaction.CommitAsync();

            return CancelOrderResult.Success;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (SqlException)
            {
                // 保留原始异常
            }
            catch (InvalidOperationException)
            {
                // 事务可能已经结束
            }

            throw;
        }
    }


    public async Task<List<int>> GetExpiredOrderIdsAsync(
        int batchSize)
    {
        if (batchSize < 1 || batchSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be between 1 and 100.");
        }

        const string sql = """
            SELECT TOP (@BatchSize)
                Id
            FROM dbo.RentalOrders
            WHERE Status = 'PendingPayment'
            AND ExpiresAt <= SYSUTCDATETIME()
            ORDER BY ExpiresAt, Id;
            """;

        var orderIds = new List<int>();

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@BatchSize",
            SqlDbType.Int).Value = batchSize;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orderIds.Add(reader.GetInt32(0));
        }

        return orderIds;
    }



    public async Task<bool> ExpireOrderAsync(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentOutOfRangeException(nameof(orderId));

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 先读取该订单关联的车辆 ID
        const string vehiclesSql = """
            SELECT VehicleId
            FROM dbo.RentalOrderItems
            WHERE OrderId = @OrderId
            ORDER BY VehicleId;
            """;

        var vehicleIds = new List<int>();

        await using (var command =
            new SqlCommand(vehiclesSql, connection))
        {
            command.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                vehicleIds.Add(reader.GetInt32(0));
            }
        }

        // 2. 开启事务
        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 3. 按照 VehicleId 升序锁定车辆
            const string lockVehicleSql = """
                SELECT Id
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            foreach (int vehicleId in vehicleIds.Distinct().OrderBy(x => x))
            {
                await using var command =
                    new SqlCommand(
                        lockVehicleSql, connection, transaction);

                command.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                if (await command.ExecuteScalarAsync() is null)
                    throw new InvalidOperationException(
                        "Order references a missing vehicle.");
            }

            // 4. 锁定订单，重新检查是否真的超时
            const string checkOrderSql = """
                SELECT Status, ExpiresAt
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId;
                """;

            bool shouldExpire = false;

            await using (var command =
                new SqlCommand(checkOrderSql, connection, transaction))
            {
                command.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string status = reader.GetString(0);

                    DateTime? expiresAt = reader.IsDBNull(1)
                        ? null
                        : reader.GetDateTime(1);

                    shouldExpire =
                        status == "PendingPayment"
                        && expiresAt.HasValue
                        && expiresAt.Value <= DateTime.UtcNow;
                }
            }

            if (!shouldExpire)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // 5. 检查所有明细是否仍处于 Reserved
            const string countSql = """
                SELECT
                    COUNT(*) AS TotalCount,
                    COALESCE(SUM(
                        CASE WHEN Status = 'Reserved'
                            THEN 1 ELSE 0 END
                    ), 0) AS ReservedCount
                FROM dbo.RentalOrderItems
                WHERE OrderId = @OrderId;
                """;

            int totalCount;
            int reservedCount;

            await using (var command =
                new SqlCommand(countSql, connection, transaction))
            {
                command.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                await using var reader =
                    await command.ExecuteReaderAsync();

                await reader.ReadAsync();

                totalCount = reader.GetInt32(0);
                reservedCount = reader.GetInt32(1);
            }

            if (totalCount == 0 ||
                totalCount != reservedCount ||
                totalCount != vehicleIds.Count)
            {
                throw new InvalidOperationException(
                    "Order items are inconsistent.");
            }

            // 6. 更新全部车辆明细
            const string updateItemsSql = """
                UPDATE dbo.RentalOrderItems
                SET Status = 'Cancelled',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE OrderId = @OrderId
                AND Status = 'Reserved';
                """;

            await using (var command =
                new SqlCommand(updateItemsSql, connection, transaction))
            {
                command.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                int affected = await command.ExecuteNonQueryAsync();

                if (affected != totalCount)
                    throw new InvalidOperationException(
                        "Unexpected number of updated order items.");
            }

            // 7. 更新订单主表，并再次限定旧状态和到期条件
            const string updateOrderSql = """
                UPDATE dbo.RentalOrders
                SET Status = 'Expired',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OrderId
                AND Status = 'PendingPayment'
                AND ExpiresAt <= SYSUTCDATETIME();
                """;

            await using (var command =
                new SqlCommand(updateOrderSql, connection, transaction))
            {
                command.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                int affected = await command.ExecuteNonQueryAsync();

                if (affected != 1)
                    throw new InvalidOperationException(
                        "Order expiration update failed.");
            }

            // 8. 成功提交
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (SqlException)
            {
                // 保留原始异常
            }
            catch (InvalidOperationException)
            {
                // 事务可能已经结束
            }

            throw;
        }
    }




    private static async Task CheckVehicleAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        RentalOrderLine line)
    {
        // 锁定车辆行，锁保持至事务结束
        const string vehicleSql = """
            SELECT Status
            FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @VehicleId;
            """;

        string? status;

        await using (var command =
            new SqlCommand(vehicleSql, connection, transaction))
        {
            command.Parameters.Add(
                "@VehicleId", SqlDbType.Int).Value =
                line.VehicleId;

            status = (string?)await command
                .ExecuteScalarAsync();
        }

        if (status != "Active")
            throw new BusinessRuleException(
                $"Vehicle {line.VehicleId} is unavailable.");

        // 判断是否与已有有效订单重叠
        const string conflictSql = """
            SELECT COUNT(*)
            FROM dbo.RentalOrderItems i
            INNER JOIN dbo.RentalOrders o
                ON o.Id = i.OrderId
            WHERE i.VehicleId = @VehicleId
              AND i.StartAt < @EndAt
              AND i.EndAt > @StartAt
              AND
              (
                  o.Status IN ('Confirmed', 'InUse')
                  OR
                  (
                      o.Status = 'PendingPayment'
                      AND o.ExpiresAt > SYSUTCDATETIME()
                  )
              );
            """;

        await using (var command =
            new SqlCommand(conflictSql, connection, transaction))
        {
            command.Parameters.Add(
                "@VehicleId", SqlDbType.Int).Value =
                line.VehicleId;

            command.Parameters.Add(
                "@StartAt", SqlDbType.DateTime2).Value =
                line.StartAt;

            command.Parameters.Add(
                "@EndAt", SqlDbType.DateTime2).Value =
                line.EndAt;

            int count = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            if (count > 0)
                throw new BusinessRuleException(
                    $"Vehicle {line.VehicleId} has a conflicting reservation.");
        }

        // 当前归还后待检查或检查中的车辆禁止预订
        const string inspectionSql = """
            SELECT COUNT(*)
            FROM dbo.RentalOrderItems
            WHERE VehicleId = @VehicleId
              AND Status IN (
                  'PendingInspection',
                  'InspectionInProgress'
              );
            """;

        await using (var command =
            new SqlCommand(inspectionSql, connection, transaction))
        {
            command.Parameters.Add(
                "@VehicleId", SqlDbType.Int).Value =
                line.VehicleId;

            int count = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            if (count > 0)
                throw new BusinessRuleException(
                    $"Vehicle {line.VehicleId} is awaiting inspection.");
        }
    }



    private static async Task<decimal> GetDailyRateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int vehicleId)
    {
        const string sql = """
            SELECT vm.DailyRate
            FROM dbo.Vehicles AS v
            INNER JOIN dbo.VehicleModels AS vm WITH (HOLDLOCK)
                ON v.VehicleModelId = vm.Id
            WHERE v.Id = @VehicleId
            AND v.Status = 'Active'
            AND vm.IsActive = 1;
            """;

        await using var command =
            new SqlCommand(sql, connection, transaction);

        command.Parameters.Add(
            "@VehicleId",
            SqlDbType.Int).Value = vehicleId;

        object? result = await command.ExecuteScalarAsync();

        if (result is null || result == DBNull.Value)
        {
            throw new BusinessRuleException(
                $"Vehicle {vehicleId} has no available rental price.");
        }

        return Convert.ToDecimal(result);
    }

}
