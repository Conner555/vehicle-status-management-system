
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly string _connectionString;

    public MaintenanceRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    // 下面依次添加 GetPendingAsync 和 ClaimAsync


    public async Task<PendingMaintenancePageDto> GetPendingAsync(
        int page,
        int pageSize)
    {
        int offset = checked((page - 1) * pageSize);

        const string sql = """
            SELECT COUNT(*)
            FROM dbo.VehicleMaintenances
            WHERE Status = 'Pending';

            SELECT
                m.Id AS MaintenanceId,
                m.VehicleId,
                v.PlateNumber,
                vm.ModelName,
                m.InspectionId,
                m.IssueDescription,
                m.Status,
                m.Priority,
                m.CreatedAt
            FROM dbo.VehicleMaintenances AS m
            JOIN dbo.Vehicles AS v
                ON v.Id = m.VehicleId
            JOIN dbo.VehicleModels AS vm
                ON vm.Id = v.VehicleModelId
            WHERE m.Status = 'Pending'
            ORDER BY
                CASE m.Priority
                    WHEN 'High' THEN 1
                    WHEN 'Medium' THEN 2
                    ELSE 3
                END,
                m.CreatedAt ASC,
                m.Id ASC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@Offset", SqlDbType.Int).Value = offset;

        command.Parameters.Add(
            "@PageSize", SqlDbType.Int).Value = pageSize;

        var result = new PendingMaintenancePageDto
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

        // 第二个结果集：任务列表
        if (!await reader.NextResultAsync())
        {
            throw new InvalidOperationException(
                "Maintenance query result is missing.");
        }

        while (await reader.ReadAsync())
        {
            result.Items.Add(new PendingMaintenanceDto
            {
                MaintenanceId = reader.GetInt32(0),
                VehicleId = reader.GetInt32(1),
                PlateNumber = reader.GetString(2),
                ModelName = reader.GetString(3),

                InspectionId = reader.IsDBNull(4)
                    ? null
                    : reader.GetInt32(4),

                IssueDescription = reader.GetString(5),
                Status = reader.GetString(6),
                Priority = reader.GetString(7),
                CreatedAt = reader.GetDateTime(8)
            });
        }

        return result;
    }


    public async Task<MaintenanceClaimResult> ClaimAsync(
        int maintenanceId,
        int operatorId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 查询维修任务对应的车辆 ID
        const string findSql = """
            SELECT VehicleId
            FROM dbo.VehicleMaintenances
            WHERE Id = @MaintenanceId;
            """;

        int vehicleId;

        await using (var cmd =
            new SqlCommand(findSql, connection))
        {
            cmd.Parameters.Add(
                "@MaintenanceId", SqlDbType.Int)
                .Value = maintenanceId;

            object? value = await cmd.ExecuteScalarAsync();

            if (value is null)
                return MaintenanceClaimResult.NotFound;

            vehicleId = Convert.ToInt32(value);
        }

        // 2. 开启事务
        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            // 3. 锁定车辆并检查运营状态
            const string vehicleSql = """
                SELECT OperationStatus
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            string? operationStatus;

            await using (var cmd = new SqlCommand(
                vehicleSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                operationStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (operationStatus != "Maintenance")
            {
                await transaction.RollbackAsync();

                return MaintenanceClaimResult.VehicleStateConflict;
            }

            // 4. 锁定维修任务
            const string maintenanceSql = """
                SELECT Status, AssignedTo
                FROM dbo.VehicleMaintenances
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @MaintenanceId
                AND VehicleId = @VehicleId;
                """;

            string? status = null;
            int? assignedTo = null;
            bool found = false;

            await using (var cmd = new SqlCommand(
                maintenanceSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@MaintenanceId", SqlDbType.Int)
                    .Value = maintenanceId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    found = true;
                    status = reader.GetString(0);

                    assignedTo = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1);
                }
            }

            if (!found)
            {
                await transaction.RollbackAsync();
                return MaintenanceClaimResult.NotFound;
            }

            if (status != "Pending" || assignedTo is not null)
            {
                await transaction.RollbackAsync();
                return MaintenanceClaimResult.AlreadyClaimed;
            }

            // 5. 原子更新维修任务
            const string updateSql = """
                UPDATE dbo.VehicleMaintenances
                SET Status = 'InProgress',
                    AssignedTo = @OperatorId,
                    StartedAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @MaintenanceId
                AND VehicleId = @VehicleId
                AND Status = 'Pending'
                AND AssignedTo IS NULL
                AND StartedAt IS NULL;
                """;

            await using (var cmd = new SqlCommand(
                updateSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@MaintenanceId", SqlDbType.Int)
                    .Value = maintenanceId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                cmd.Parameters.Add(
                    "@OperatorId", SqlDbType.Int)
                    .Value = operatorId;

                int affectedRows = await cmd.ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    throw new InvalidOperationException(
                        "Maintenance claim update failed.");
                }
            }

            await transaction.CommitAsync();

            return MaintenanceClaimResult.Success;
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


    public async Task<MaintenanceSubmitResult> SubmitRepairAsync(
        int maintenanceId,
        int operatorId,
        string repairNotes,
        decimal repairCost)
    {
        if (maintenanceId <= 0 || operatorId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maintenanceId));

        if (string.IsNullOrWhiteSpace(repairNotes) ||
            repairNotes.Length > 1000 ||
            repairCost < 0 || repairCost > 9999999999.99m)
            throw new ArgumentException(
                "Invalid repair data.");

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 查询关联车辆 ID
        const string findSql = """
            SELECT VehicleId
            FROM dbo.VehicleMaintenances
            WHERE Id = @MaintenanceId;
            """;

        int vehicleId;

        await using (var cmd =
            new SqlCommand(findSql, connection))
        {
            cmd.Parameters.Add(
                "@MaintenanceId", SqlDbType.Int
            ).Value = maintenanceId;

            object? value = await cmd.ExecuteScalarAsync();

            if (value is null)
                return MaintenanceSubmitResult.NotFound;

            vehicleId = Convert.ToInt32(value);
        }

        // 2. 开启事务
        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            // 3. 锁定车辆，校验运营状态
            const string vehicleSql = """
                SELECT OperationStatus
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            string? operationStatus;

            await using (var cmd = new SqlCommand(
                vehicleSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int
                ).Value = vehicleId;

                operationStatus =
                    (string?)await cmd.ExecuteScalarAsync();
            }

            if (operationStatus != "Maintenance")
            {
                await transaction.RollbackAsync();
                return MaintenanceSubmitResult.VehicleStateConflict;
            }

            // 4. 锁维修任务并检查状态和领取人
            const string maintenanceSql = """
                SELECT Status, AssignedTo
                FROM dbo.VehicleMaintenances
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @MaintenanceId
                AND VehicleId = @VehicleId;
                """;

            string? status = null;
            int? assignedTo = null;
            bool found = false;

            await using (var cmd = new SqlCommand(
                maintenanceSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@MaintenanceId", SqlDbType.Int
                ).Value = maintenanceId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int
                ).Value = vehicleId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    found = true;
                    status = reader.GetString(0);
                    assignedTo = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1);
                }
            }

            if (!found)
            {
                await transaction.RollbackAsync();
                return MaintenanceSubmitResult.NotFound;
            }

            if (status != "InProgress")
            {
                await transaction.RollbackAsync();
                return MaintenanceSubmitResult.NotInProgress;
            }

            if (assignedTo != operatorId)
            {
                await transaction.RollbackAsync();
                return MaintenanceSubmitResult.Forbidden;
            }

            // 5. 仅更新维修任务
            const string updateSql = """
                UPDATE dbo.VehicleMaintenances
                SET Status = 'AwaitingVerification',
                    RepairNotes = @RepairNotes,
                    RepairCost = @RepairCost,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @MaintenanceId
                AND VehicleId = @VehicleId
                AND Status = 'InProgress'
                AND AssignedTo = @OperatorId
                AND StartedAt IS NOT NULL
                AND CompletedAt IS NULL;
                """;

            await using (var cmd = new SqlCommand(
                updateSql, connection, transaction))
            {
                cmd.Parameters.Add(
                    "@MaintenanceId", SqlDbType.Int
                ).Value = maintenanceId;

                cmd.Parameters.Add(
                    "@VehicleId", SqlDbType.Int
                ).Value = vehicleId;

                cmd.Parameters.Add(
                    "@OperatorId", SqlDbType.Int
                ).Value = operatorId;

                cmd.Parameters.Add(
                    "@RepairNotes", SqlDbType.NVarChar, 1000
                ).Value = repairNotes;

                var costParam = cmd.Parameters.Add(
                    "@RepairCost", SqlDbType.Decimal);

                costParam.Precision = 12;
                costParam.Scale = 2;
                costParam.Value = repairCost;

                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected != 1)
                    throw new InvalidOperationException(
                        "Maintenance submission update failed.");
            }

            await transaction.CommitAsync();

            return MaintenanceSubmitResult.Success;
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

    //admin管理员完成符复核任务
    public async Task<MaintenanceVerifyResult> VerifyAsync(
        int maintenanceId,
        int verifierId,
        bool approved,
        string verificationNotes)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        // 1. 查询维修任务关联车辆
        const string findSql = """
            SELECT VehicleId
            FROM dbo.VehicleMaintenances
            WHERE Id = @MaintenanceId;
            """;

        int vehicleId;

        await using (var cmd = new SqlCommand(findSql, connection))
        {
            cmd.Parameters.Add("@MaintenanceId", SqlDbType.Int)
                .Value = maintenanceId;

            object? value = await cmd.ExecuteScalarAsync();

            if (value is null)
                return MaintenanceVerifyResult.NotFound;

            vehicleId = Convert.ToInt32(value);
        }

        // 2. 开启事务
        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        async Task<MaintenanceVerifyResult> AbortAsync(
            MaintenanceVerifyResult reason)
        {
            await transaction.RollbackAsync();
            return reason;
        }

        try
        {
            // 3. 锁定车辆
            const string vehicleSql = """
                SELECT Status, OperationStatus
                FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @VehicleId;
                """;

            string? managementStatus = null;
            string? operationStatus = null;

            await using (var cmd = new SqlCommand(
                vehicleSql, connection, transaction))
            {
                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    managementStatus = reader.GetString(0);
                    operationStatus = reader.GetString(1);
                }
            }

            if (operationStatus != "Maintenance")
                return await AbortAsync(
                    MaintenanceVerifyResult.VehicleStateConflict);

            // 4. 锁维修任务，读取负责人和状态
            const string maintenanceSql = """
                SELECT Status, AssignedTo
                FROM dbo.VehicleMaintenances
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @MaintenanceId
                AND VehicleId = @VehicleId;
                """;

            string? maintenanceStatus = null;
            int? assignedTo = null;

            await using (var cmd = new SqlCommand(
                maintenanceSql, connection, transaction))
            {
                cmd.Parameters.Add("@MaintenanceId", SqlDbType.Int)
                    .Value = maintenanceId;

                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    maintenanceStatus = reader.GetString(0);
                    assignedTo = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1);
                }
            }

            if (maintenanceStatus is null)
                return await AbortAsync(
                    MaintenanceVerifyResult.NotFound);

            if (maintenanceStatus != "AwaitingVerification")
                return await AbortAsync(
                    MaintenanceVerifyResult.InvalidStatus);

            // 禁止维修人员自己复核
            if (assignedTo == verifierId)
                return await AbortAsync(
                    MaintenanceVerifyResult.Forbidden);

            // 5. Approved 需要额外检查运营条件
            if (approved)
            {
                if (managementStatus != "Active")
                    return await AbortAsync(
                        MaintenanceVerifyResult.VehicleStateConflict);

                const string blockerSql = """
                    SELECT
                        CASE WHEN EXISTS (
                            SELECT 1
                            FROM dbo.RentalOrderItems
                            WHERE VehicleId = @VehicleId
                            AND Status IN (
                                'InUse', 'PendingInspection'
                            )
                        )
                        THEN 1 ELSE 0 END;
                    """;

                await using var blockerCmd = new SqlCommand(
                    blockerSql, connection, transaction);

                blockerCmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                int hasBlocker = Convert.ToInt32(
                    await blockerCmd.ExecuteScalarAsync());

                if (hasBlocker == 1)
                    return await AbortAsync(
                        MaintenanceVerifyResult.VehicleStateConflict);
            }

            // 6. 更新维修任务
            const string updateMaintenanceSql = """
                UPDATE dbo.VehicleMaintenances
                SET Status = @NewStatus,
                    VerifiedBy = @VerifierId,
                    VerifiedAt = SYSUTCDATETIME(),
                    VerificationNotes = @Notes,
                    CompletedAt = CASE
                        WHEN @Approved = 1
                        THEN SYSUTCDATETIME()
                        ELSE NULL
                    END,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @MaintenanceId
                AND VehicleId = @VehicleId
                AND Status = 'AwaitingVerification'
                AND AssignedTo IS NOT NULL
                AND AssignedTo <> @VerifierId
                AND StartedAt IS NOT NULL
                AND CompletedAt IS NULL;
                """;

            await using (var cmd = new SqlCommand(
                updateMaintenanceSql, connection, transaction))
            {
                cmd.Parameters.Add("@MaintenanceId", SqlDbType.Int)
                    .Value = maintenanceId;

                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                cmd.Parameters.Add("@VerifierId", SqlDbType.Int)
                    .Value = verifierId;

                cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, 1000)
                    .Value = verificationNotes;

                cmd.Parameters.Add("@Approved", SqlDbType.Bit)
                    .Value = approved;

                cmd.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 30)
                    .Value = approved ? "Completed" : "InProgress";

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Maintenance verification update failed.");
            }

            // 7. 仅复核通过时恢复车辆运营
            if (approved)
            {
                const string updateVehicleSql = """
                    UPDATE dbo.Vehicles
                    SET OperationStatus = 'Available'
                    WHERE Id = @VehicleId
                    AND Status = 'Active'
                    AND OperationStatus = 'Maintenance';
                    """;

                await using var cmd = new SqlCommand(
                    updateVehicleSql, connection, transaction);

                cmd.Parameters.Add("@VehicleId", SqlDbType.Int)
                    .Value = vehicleId;

                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Vehicle recovery update failed.");
            }

            await transaction.CommitAsync();

            return approved
                ? MaintenanceVerifyResult.Approved
                : MaintenanceVerifyResult.Rejected;
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
