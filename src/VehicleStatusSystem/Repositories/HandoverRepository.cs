using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class HandoverRepository : IHandoverRepository
{
    private readonly string _connectionString;

    public HandoverRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<HandoverResult> HandoverAsync(
        int orderId,
        int orderItemId,
        int operatorId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 获取本次交付的车辆 ID
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
                return HandoverResult.NotFound;

            vehicleId = Convert.ToInt32(value);
        }

        // 2. 开始事务
        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 3. 锁定车辆

            const string vehicleSql = """
                SELECT Status, OperationStatus
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            string? vehicleStatus = null;
            string? operationStatus = null;

            await using (var cmd =
                new SqlCommand(vehicleSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    vehicleStatus = reader.GetString(0);

                    operationStatus = reader.IsDBNull(1)
                        ? null
                        : reader.GetString(1);
                }
            }

            if (vehicleStatus != "Active" ||
                operationStatus != "Available")
            {
                return await AbortAsync(
                    transaction,
                    HandoverResult.VehicleUnavailable);
            }


            // 4. 锁定订单，验证支付成功
            const string orderSql = """
                SELECT Status
                FROM dbo.RentalOrders WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @OrderId;
                """;

            string? orderStatus;

            await using (var cmd =
                new SqlCommand(orderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                orderStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (orderStatus is null)
                return await AbortAsync(
                    transaction, HandoverResult.NotFound);

            if (orderStatus != "Confirmed" &&
                orderStatus != "InUse")
            {
                return await AbortAsync(
                    transaction, HandoverResult.InvalidOrderStatus);
            }

            // 5. 锁定明细，读取租期和状态
            const string itemSql = """
                SELECT Status, StartAt, EndAt
                FROM dbo.RentalOrderItems WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @ItemId
                  AND OrderId = @OrderId
                  AND VehicleId = @VehicleId;
                """;

            string? itemStatus = null;
            DateTime startAt = default;
            DateTime endAt = default;

            await using (var cmd =
                new SqlCommand(itemSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int).Value = orderItemId;
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    itemStatus = reader.GetString(0);
                    startAt = reader.GetDateTime(1);
                    endAt = reader.GetDateTime(2);
                }
            }

            if (itemStatus is null)
                return await AbortAsync(
                    transaction, HandoverResult.NotFound);

            if (itemStatus != "Reserved")
                return await AbortAsync(
                    transaction, HandoverResult.InvalidItemStatus);

            // 6. 检查实际交车时间
            const string nowSql = "SELECT SYSUTCDATETIME();";

            DateTime now;

            await using (var cmd =
                new SqlCommand(nowSql, connection, transaction))
            {
                now = Convert.ToDateTime(
                    await cmd.ExecuteScalarAsync());
            }

            if (now < startAt.AddMinutes(-30) || now >= endAt)
                return await AbortAsync(
                    transaction, HandoverResult.OutsideHandoverWindow);

            // 7. 不能交付正在其他订单中实际使用或验车的车辆
            const string occupiedSql = """
                SELECT COUNT(*)
                FROM dbo.RentalOrderItems
                WHERE VehicleId = @VehicleId
                  AND Id <> @ItemId
                  AND Status IN (
                      'InUse',
                      'PendingInspection',
                      'InspectionInProgress'
                  );
                """;

            await using (var cmd =
                new SqlCommand(occupiedSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int).Value = vehicleId;
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int).Value = orderItemId;

                int occupied = Convert.ToInt32(
                    await cmd.ExecuteScalarAsync());

                if (occupied > 0)
                    return await AbortAsync(
                        transaction, HandoverResult.VehicleUnavailable);
            }

            // 8. 更新当前车辆明细
            const string updateItemSql = """
                UPDATE dbo.RentalOrderItems
                SET Status = 'InUse',
                    ActualHandedOverAt = SYSUTCDATETIME(),
                    HandedOverBy = @OperatorId,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @ItemId
                  AND OrderId = @OrderId
                  AND Status = 'Reserved';
                """;

            await using (var cmd =
                new SqlCommand(updateItemSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@ItemId", SqlDbType.Int).Value = orderItemId;
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;
                cmd.Parameters.Add(
                    "@OperatorId", SqlDbType.Int).Value = operatorId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Handover item update failed.");
            }

            // 9. 更新订单整体状态
            const string updateOrderSql = """
                UPDATE dbo.RentalOrders
                SET Status = 'InUse',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OrderId
                  AND Status IN ('Confirmed', 'InUse');
                """;

            await using (var cmd =
                new SqlCommand(updateOrderSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@OrderId", SqlDbType.Int).Value = orderId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Handover order update failed.");
            }


            // 10.更新车辆当前运营状态
            const string updateOperationSql = """
                UPDATE dbo.Vehicles
                SET OperationStatus = 'InUse'
                WHERE Id = @VehicleId
                AND Status = 'Active'
                AND OperationStatus = 'Available';
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
                        "Vehicle operation status update failed.");
                }
            }

            await transaction.CommitAsync();
            return HandoverResult.Success;
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

    private static async Task<HandoverResult> AbortAsync(
        SqlTransaction transaction,
        HandoverResult result)
    {
        await transaction.RollbackAsync();
        return result;
    }
}
