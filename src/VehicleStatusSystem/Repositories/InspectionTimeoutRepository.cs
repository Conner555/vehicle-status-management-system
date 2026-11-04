
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.BackgroundServices;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public sealed class InspectionTimeoutRepository
    : IInspectionTimeoutRepository
{
    private readonly string _connectionString;

    public InspectionTimeoutRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<IReadOnlyList<int>>
        GetOverdueInspectionIdsAsync(
            int limit,
            CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit));

        const string sql = """
            SELECT TOP (@Limit)
                vi.Id
            FROM dbo.VehicleInspections AS vi
            WHERE vi.Status IN ('Pending', 'InProgress')
              AND vi.ReturnedAt <
                  DATEADD(HOUR, -12, SYSUTCDATETIME())
            ORDER BY vi.ReturnedAt, vi.Id;
            """;

        var ids = new List<int>();

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@Limit", SqlDbType.Int).Value = limit;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }


    public async Task<InspectionTimeoutSyncResult> SynchronizeEventsAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        const string sql = """
            SET XACT_ABORT ON;

            -- 1. 发现新超时任务
            ;WITH NewOverdue AS
            (
                SELECT TOP (@BatchSize)
                    vi.Id,
                    DATEADD(HOUR, 12, vi.ReturnedAt) AS DeadlineAt
                FROM dbo.VehicleInspections vi
                WHERE vi.Status IN ('Pending', 'InProgress')
                AND vi.ReturnedAt IS NOT NULL
                AND vi.InspectionDeadlineAt <= SYSUTCDATETIME()

                AND NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.InspectionTimeoutEvents e
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE e.InspectionId = vi.Id
                )
                ORDER BY vi.ReturnedAt, vi.Id
            )
            INSERT INTO dbo.InspectionTimeoutEvents
            (
                InspectionId,
                DeadlineAt,
                Status
            )
            SELECT
                Id,
                DeadlineAt,
                N'Open'
            FROM NewOverdue;

            SELECT @@ROWCOUNT AS CreatedCount;

            -- 2. 已完成验车，关闭事件
            UPDATE e
            SET
                e.Status = N'Resolved',
                e.ResolvedAt = SYSUTCDATETIME()
            FROM dbo.InspectionTimeoutEvents e
            INNER JOIN dbo.VehicleInspections vi
                ON vi.Id = e.InspectionId
            WHERE e.Status = N'Open'
            AND vi.Status NOT IN ('Pending', 'InProgress');

            SELECT @@ROWCOUNT AS ResolvedCount;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await using var command =
                new SqlCommand(sql, connection, transaction);

            command.Parameters.Add(
                "@BatchSize", SqlDbType.Int).Value = batchSize;

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new DataException("Created count is missing.");

            int created = reader.GetInt32(0);

            if (!await reader.NextResultAsync(cancellationToken))
                throw new DataException("Resolved result set is missing.");

            if (!await reader.ReadAsync(cancellationToken))
                throw new DataException("Resolved count is missing.");

            int resolved = reader.GetInt32(0);

            await reader.CloseAsync();

            await transaction.CommitAsync(cancellationToken);

            return new InspectionTimeoutSyncResult(
                created,
                resolved);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
