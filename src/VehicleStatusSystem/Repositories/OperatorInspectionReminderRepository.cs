
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Repositories;

public sealed class OperatorInspectionReminderRepository
    : IOperatorInspectionReminderRepository
{
    private readonly string _connectionString;

    public OperatorInspectionReminderRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    private const string SelectColumns = """
        e.Id,
        e.InspectionId,
        vi.RentalOrderItemId AS OrderItemId,
        vi.VehicleId,
        v.PlateNumber,
        vi.Status AS InspectionStatus,
        vi.OperatorId,
        e.ScheduledAt,
        vi.InspectionDeadlineAt AS DeadlineAt,
        e.DetectedAt,
        e.Status,
        e.ResolvedAt
        """;

    private const string FromTables = """
        FROM dbo.InspectionReminderEvents e
        INNER JOIN dbo.VehicleInspections vi
            ON vi.Id = e.InspectionId
        INNER JOIN dbo.Vehicles v
            ON v.Id = vi.VehicleId
        """;

    public async Task<InspectionReminderPageDto> GetPageAsync(
        string? status,
        int page,
        int pageSize,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        string sql = $"""
            SELECT COUNT(*)
            {FromTables}
            WHERE (@Status IS NULL OR e.Status = @Status)
              AND (
                  @IsAdmin = 1
                  OR vi.OperatorId = @OperatorId
                  OR (
                      e.Status = N'Open'
                      AND vi.Status = 'Pending'
                      AND vi.OperatorId IS NULL
                  )
              );

            SELECT {SelectColumns}
            {FromTables}
            WHERE (@Status IS NULL OR e.Status = @Status)
              AND (
                  @IsAdmin = 1
                  OR vi.OperatorId = @OperatorId
                  OR (
                      e.Status = N'Open'
                      AND vi.Status = 'Pending'
                      AND vi.OperatorId IS NULL
                  )
              )
            ORDER BY e.DetectedAt DESC, e.Id DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        int offset = checked((page - 1) * pageSize);

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        AddAccessParameters(
            command, status, operatorId, isAdmin);

        command.Parameters.Add(
            "@Offset", SqlDbType.Int).Value = offset;

        command.Parameters.Add(
            "@PageSize", SqlDbType.Int).Value = pageSize;

        var result = new InspectionReminderPageDto
        {
            Page = page,
            PageSize = pageSize
        };

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new DataException(
                "Reminder count result is missing.");

        result.TotalCount = reader.GetInt32(0);

        if (!await reader.NextResultAsync(cancellationToken))
            throw new DataException(
                "Reminder items result is missing.");

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Items.Add(MapReminder(reader));
        }

        return result;
    }

    public async Task<InspectionReminderDto?> GetByIdAsync(
        long reminderId,
        int operatorId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        string sql = $"""
            SELECT {SelectColumns}
            {FromTables}
            WHERE e.Id = @ReminderId
              AND (
                  @IsAdmin = 1
                  OR vi.OperatorId = @OperatorId
                  OR (
                      e.Status = N'Open'
                      AND vi.Status = 'Pending'
                      AND vi.OperatorId IS NULL
                  )
              );
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        AddAccessParameters(
            command, null, operatorId, isAdmin);

        command.Parameters.Add(
            "@ReminderId", SqlDbType.BigInt).Value = reminderId;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapReminder(reader)
            : null;
    }

    private static void AddAccessParameters(
        SqlCommand command,
        string? status,
        int operatorId,
        bool isAdmin)
    {
        command.Parameters.Add(
            "@Status", SqlDbType.NVarChar, 20).Value =
            (object?)status ?? DBNull.Value;

        command.Parameters.Add(
            "@OperatorId", SqlDbType.Int).Value = operatorId;

        command.Parameters.Add(
            "@IsAdmin", SqlDbType.Bit).Value = isAdmin;
    }

    private static InspectionReminderDto MapReminder(
        SqlDataReader reader)
    {
        int operatorIndex = reader.GetOrdinal("OperatorId");
        int resolvedIndex = reader.GetOrdinal("ResolvedAt");
        int deadlineIndex = reader.GetOrdinal("DeadlineAt");

        return new InspectionReminderDto
        {
            Id = reader.GetInt64(
                reader.GetOrdinal("Id")),

            InspectionId = reader.GetInt32(
                reader.GetOrdinal("InspectionId")),

            OrderItemId = reader.GetInt32(
                reader.GetOrdinal("OrderItemId")),

            VehicleId = reader.GetInt32(
                reader.GetOrdinal("VehicleId")),

            PlateNumber = reader.GetString(
                reader.GetOrdinal("PlateNumber")),

            InspectionStatus = reader.GetString(
                reader.GetOrdinal("InspectionStatus")),

            OperatorId = reader.IsDBNull(operatorIndex)
                ? null
                : reader.GetInt32(operatorIndex),

            ScheduledAt = reader.GetDateTime(
                reader.GetOrdinal("ScheduledAt")),

            DeadlineAt = reader.GetDateTime(deadlineIndex),

            DetectedAt = reader.GetDateTime(
                reader.GetOrdinal("DetectedAt")),

            Status = reader.GetString(
                reader.GetOrdinal("Status")),

            ResolvedAt = reader.IsDBNull(resolvedIndex)
                ? null
                : reader.GetDateTime(resolvedIndex)
        };
    }
}
