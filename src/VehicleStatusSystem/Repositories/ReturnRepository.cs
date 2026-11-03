
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;
using VehicleStatusSystem.Domain;

namespace VehicleStatusSystem.Repositories;

public class ReturnRepository : IReturnRepository
{
    private readonly string _connectionString;

    public ReturnRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<ReturnVehicleResult> ReturnAsync(
        int orderId,
        int orderItemId,
        int customerId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 获取目标租赁明细对应的车辆
        const string findVehicleSql = """
            SELECT VehicleId
            FROM dbo.RentalOrderItems
            WHERE Id = @ItemId
              AND OrderId = @OrderId;
            """;

        int vehicleId;

        await using (var cmd =
            new SqlCommand(findVehicleSql, connection))
        {
            cmd.Parameters.Add(
                "@ItemId", SqlDbType.Int).Value = orderItemId;

            cmd.Parameters.Add(
                "@OrderId", SqlDbType.Int).Value = orderId;

            object? value = await cmd.ExecuteScalarAsync();

            if (value is null)
                return ReturnVehicleResult.NotFound;

            vehicleId = Convert.ToInt32(value);
        }

        // 2. 开启事务
        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 3. 锁定车辆，沿用统一的锁定顺序

            const string lockVehicleSql = """
                SELECT OperationStatus
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            string? operationStatus;

            await using (var cmd =
                new SqlCommand(
                    lockVehicleSql,
                    connection,
                    transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                operationStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (operationStatus != "InUse")
            {
                return await AbortAsync(
                    transaction,
                    ReturnVehicleResult.InvalidItemStatus);
            }

            // 4. 锁定订单，同时确认订单所属用户
            const string orderSql = """
                SELECT Status
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId
                  AND CustomerId = @CustomerId;
                """;

            string? orderStatus;

            await using (var cmd =
                new SqlCommand(
                    orderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add(
                    "@CustomerId", SqlDbType.Int).Value = customerId;

                orderStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (orderStatus is null)
            {
                await transaction.RollbackAsync();
                return ReturnVehicleResult.NotFound;
            }

            if (orderStatus != "InUse")
            {
                await transaction.RollbackAsync();
                return ReturnVehicleResult.InvalidOrderStatus;
            }

            // 5. 锁定对应的订单明细
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
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int).Value = orderItemId;

                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                itemStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (itemStatus is null)
            {
                await transaction.RollbackAsync();
                return ReturnVehicleResult.NotFound;
            }

            if (itemStatus != "InUse")
            {
                await transaction.RollbackAsync();
                return ReturnVehicleResult.InvalidItemStatus;
            }

            DateTime returnedAtUtc = DateTime.UtcNow;

            var schedule = InspectionScheduleCalculator.Generate(
                returnedAtUtc);

            // 6. 更新归还状态和实际归还时间
            const string updateSql = """
                UPDATE dbo.RentalOrderItems
                SET Status = 'PendingInspection',
                    ActualReturnedAt = @ReturnedAt,
                    UpdatedAt = @ReturnedAt
                WHERE Id = @ItemId
                AND OrderId = @OrderId
                AND Status = 'InUse'
                AND ActualReturnedAt IS NULL;
                """;

            await using (var cmd =
                new SqlCommand(updateSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int).Value = orderItemId;

                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                cmd.Parameters.Add(
                    "@ReturnedAt", SqlDbType.DateTime2).Value =
                    returnedAtUtc;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        "Vehicle return update failed.");
                }
            }

            // 7. 创建验车任务
            // 计划时间随机生成，硬截止为归还后 12 小时
            // 与车辆归还操作处于同一事务
            const string insertInspectionSql = """
                INSERT INTO dbo.VehicleInspections
                (
                    RentalOrderItemId,
                    VehicleId,
                    ReturnedAt,
                    InspectionDueAt,
                    ScheduledInspectionAt,
                    InspectionDeadlineAt,
                    Status
                )
                VALUES
                (
                    @ItemId,
                    @VehicleId,
                    @ReturnedAt,
                    @ScheduledInspectionAt,
                    @ScheduledInspectionAt,
                    @InspectionDeadlineAt,
                    'Pending'
                );
                """;

            await using (var cmd =
                new SqlCommand(
                    insertInspectionSql,
                    connection,
                    transaction))
            {
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int).Value = orderItemId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                cmd.Parameters.Add(
                    "@ReturnedAt", SqlDbType.DateTime2).Value =
                    returnedAtUtc;

                cmd.Parameters.Add(
                    "@ScheduledInspectionAt",
                    SqlDbType.DateTime2).Value =
                    schedule.ScheduledAt;

                cmd.Parameters.Add(
                    "@InspectionDeadlineAt",
                    SqlDbType.DateTime2).Value =
                    schedule.DeadlineAt;

                int affectedRows =
                    await cmd.ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    throw new InvalidOperationException(
                        "Inspection task creation failed.");
                }
            }


            //更新车辆运营状态（还车后进入operator检查）
            const string updateOperationSql = """
                UPDATE dbo.Vehicles
                SET OperationStatus = 'PendingInspection'
                WHERE Id = @VehicleId
                AND OperationStatus = 'InUse';
                """;

            await using (var cmd =
                new SqlCommand(
                    updateOperationSql,
                    connection,
                    transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        "Vehicle return status update failed.");
                }
            }

            // 9. 不更新订单为 Completed
            // 后续必须等待所有车辆验车完成

            await transaction.CommitAsync();

            return ReturnVehicleResult.Success;
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

    private static async Task<ReturnVehicleResult> AbortAsync(
        SqlTransaction transaction,
        ReturnVehicleResult result)
    {
        await transaction.RollbackAsync();
        return result;
    }

}
