
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Repositories;

public class VehicleAvailabilityRepository
    : IVehicleAvailabilityRepository
{
    private readonly string _connectionString;

    public VehicleAvailabilityRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<AvailabilityPageDto> GetAvailableAsync(
        DateTime startAtUtc,
        DateTime endAtUtc,
        int? modelId,
        int page,
        int pageSize)
    {
        int offset = checked((page - 1) * pageSize);

        const string sql = """
            DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

            -- 查询符合条件的车辆总数
            SELECT COUNT(*)
            FROM dbo.Vehicles AS v
            INNER JOIN dbo.VehicleModels AS vm
                ON vm.Id = v.VehicleModelId
            WHERE
                v.Status = 'Active'
                AND v.OperationStatus = 'Available'
                AND vm.IsActive = 1
                AND (@ModelId IS NULL OR vm.Id = @ModelId)

                AND NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RentalOrderItems AS busy
                    WHERE busy.VehicleId = v.Id
                      AND busy.Status IN (
                          'InUse',
                          'PendingInspection',
                          'InspectionInProgress'
                      )
                )

                AND NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RentalOrderItems AS i
                    INNER JOIN dbo.RentalOrders AS o
                        ON o.Id = i.OrderId
                    WHERE i.VehicleId = v.Id
                      AND i.StartAt < @EndAtUtc
                      AND i.EndAt > @StartAtUtc
                      AND (
                          o.Status IN ('Confirmed', 'InUse')
                          OR (
                              o.Status = 'PendingPayment'
                              AND o.ExpiresAt > @Now
                          )
                      )
                );

            -- 查询当前页数据
            SELECT
                v.Id AS VehicleId,
                v.PlateNumber,
                vm.Id AS ModelId,
                vm.ModelName,
                vm.DailyRate,
                v.Status,
                v.OperationStatus
            FROM dbo.Vehicles AS v
            INNER JOIN dbo.VehicleModels AS vm
                ON vm.Id = v.VehicleModelId
            WHERE
                v.Status = 'Active'
                AND v.OperationStatus = 'Available'
                AND vm.IsActive = 1
                AND (@ModelId IS NULL OR vm.Id = @ModelId)

                AND NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RentalOrderItems AS busy
                    WHERE busy.VehicleId = v.Id
                      AND busy.Status IN (
                          'InUse',
                          'PendingInspection',
                          'InspectionInProgress'
                      )
                )

                AND NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.RentalOrderItems AS i
                    INNER JOIN dbo.RentalOrders AS o
                        ON o.Id = i.OrderId
                    WHERE i.VehicleId = v.Id
                      AND i.StartAt < @EndAtUtc
                      AND i.EndAt > @StartAtUtc
                      AND (
                          o.Status IN ('Confirmed', 'InUse')
                          OR (
                              o.Status = 'PendingPayment'
                              AND o.ExpiresAt > @Now
                          )
                      )
                )
            ORDER BY v.Id
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        // 所有 SQL 参数使用明确的数据类型
        command.Parameters.Add(
            "@StartAtUtc", SqlDbType.DateTime2).Value =
            startAtUtc;

        command.Parameters.Add(
            "@EndAtUtc", SqlDbType.DateTime2).Value =
            endAtUtc;

        command.Parameters.Add(
            "@ModelId", SqlDbType.Int).Value =
            (object?)modelId ?? DBNull.Value;

        command.Parameters.Add(
            "@Offset", SqlDbType.Int).Value = offset;

        command.Parameters.Add(
            "@PageSize", SqlDbType.Int).Value = pageSize;

        var result = new AvailabilityPageDto
        {
            Page = page,
            PageSize = pageSize
        };

        await using var reader =
            await command.ExecuteReaderAsync();

        // 结果集 1：车辆总数
        if (!await reader.ReadAsync())
            throw new DataException(
                "Availability count result is missing.");

        result.TotalCount = reader.GetInt32(0);

        // 结果集 2：当前页车辆
        if (!await reader.NextResultAsync())
            throw new DataException(
                "Availability data result is missing.");

        while (await reader.ReadAsync())
        {
            result.Items.Add(new AvailableVehicleDto
            {
                VehicleId = reader.GetInt32(
                    reader.GetOrdinal("VehicleId")),

                PlateNumber = reader.GetString(
                    reader.GetOrdinal("PlateNumber")),

                ModelId = reader.GetInt32(
                    reader.GetOrdinal("ModelId")),

                ModelName = reader.GetString(
                    reader.GetOrdinal("ModelName")),

                DailyRate = reader.GetDecimal(
                    reader.GetOrdinal("DailyRate")),

                Status = reader.GetString(
                    reader.GetOrdinal("Status")),

                OperationStatus = reader.GetString(
                    reader.GetOrdinal("OperationStatus"))
            });
        }

        return result;
    }
}
