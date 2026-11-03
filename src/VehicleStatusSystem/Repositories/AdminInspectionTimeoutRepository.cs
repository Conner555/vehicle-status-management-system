
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Repositories;

public sealed class AdminInspectionTimeoutRepository
    : IAdminInspectionTimeoutRepository
{
    private readonly string _connectionString;

    public AdminInspectionTimeoutRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string is missing.");
    }

    private const string FromSql = """
        FROM dbo.InspectionTimeoutEvents e
        INNER JOIN dbo.VehicleInspections vi
            ON vi.Id = e.InspectionId
        INNER JOIN dbo.Vehicles v
            ON v.Id = vi.VehicleId
        """;

    private const string ColumnsSql = """
        e.Id,
        e.InspectionId,
        vi.RentalOrderItemId AS OrderItemId,
        vi.VehicleId,
        v.PlateNumber,
        vi.Status AS InspectionStatus,
        e.DeadlineAt,
        e.DetectedAt,
        e.Status,
        e.ResolvedAt
        """;

    public async Task<InspectionTimeoutPageDto> GetPageAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.InspectionTimeoutEvents e
            WHERE (@Status IS NULL OR e.Status = @Status);

            SELECT
                e.Id,
                e.InspectionId,
                vi.RentalOrderItemId AS OrderItemId,
                vi.VehicleId,
                v.PlateNumber,
                vi.Status AS InspectionStatus,
                e.DeadlineAt,
                e.DetectedAt,
                e.Status,
                e.ResolvedAt
            FROM dbo.InspectionTimeoutEvents e
            INNER JOIN dbo.VehicleInspections vi
                ON vi.Id = e.InspectionId
            INNER JOIN dbo.Vehicles v
                ON v.Id = vi.VehicleId
            WHERE (@Status IS NULL OR e.Status = @Status)
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

        command.Parameters.Add(
            "@Status", SqlDbType.NVarChar, 20).Value =
            (object?)status ?? DBNull.Value;

        command.Parameters.Add(
            "@Offset", SqlDbType.Int).Value = offset;

        command.Parameters.Add(
            "@PageSize", SqlDbType.Int).Value = pageSize;

        var result = new InspectionTimeoutPageDto
        {
            Page = page,
            PageSize = pageSize
        };

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new DataException("Count result missing.");

        result.TotalCount = reader.GetInt32(0);

        if (!await reader.NextResultAsync(cancellationToken))
            throw new DataException("Items result missing.");

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Items.Add(MapEvent(reader));
        }

        return result;
    }

    public async Task<InspectionTimeoutEventDto?> GetByIdAsync(
        long eventId,
        CancellationToken cancellationToken)
    {
        string sql = $"""
            SELECT {ColumnsSql}
            {FromSql}
            WHERE e.Id = @EventId;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EventId", SqlDbType.BigInt).Value = eventId;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapEvent(reader)
            : null;
    }

    private static InspectionTimeoutEventDto MapEvent(
        SqlDataReader reader)
    {
        int resolvedIndex =
            reader.GetOrdinal("ResolvedAt");

        return new InspectionTimeoutEventDto
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

            DeadlineAt = reader.GetDateTime(
                reader.GetOrdinal("DeadlineAt")),

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
