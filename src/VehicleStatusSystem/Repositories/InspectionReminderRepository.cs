
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public sealed class InspectionReminderRepository
    : IInspectionReminderRepository
{
    private readonly string _connectionString;

    public InspectionReminderRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string is missing.");
    }

    public async Task<InspectionReminderSyncResult> SynchronizeAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

            -- 1. 创建新提醒
            INSERT INTO dbo.InspectionReminderEvents
            (
                InspectionId,
                ScheduledAt,
                Status
            )
            SELECT
                vi.Id,
                vi.ScheduledInspectionAt,
                N'Open'
            FROM dbo.VehicleInspections vi
            WHERE vi.Status IN ('Pending', 'InProgress')
              AND vi.ScheduledInspectionAt <= @Now
              AND vi.InspectionDeadlineAt > @Now
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.InspectionReminderEvents e
                      WITH (UPDLOCK, HOLDLOCK)
                  WHERE e.InspectionId = vi.Id
              );

            DECLARE @Created INT = @@ROWCOUNT;

            -- 2. 验车完成：关闭提醒
            UPDATE e
            SET
                e.Status = N'Resolved',
                e.ResolvedAt = @Now
            FROM dbo.InspectionReminderEvents e
            INNER JOIN dbo.VehicleInspections vi
                ON vi.Id = e.InspectionId
            WHERE e.Status = N'Open'
              AND vi.Status IN ('Passed', 'Failed');

            DECLARE @Resolved INT = @@ROWCOUNT;

            -- 3. 超过硬截止：升级事件
            UPDATE e
            SET
                e.Status = N'Escalated',
                e.ResolvedAt = @Now
            FROM dbo.InspectionReminderEvents e
            INNER JOIN dbo.VehicleInspections vi
                ON vi.Id = e.InspectionId
            WHERE e.Status = N'Open'
              AND vi.Status IN ('Pending', 'InProgress')
              AND vi.InspectionDeadlineAt <= @Now;

            DECLARE @Escalated INT = @@ROWCOUNT;

            SELECT
                @Created AS CreatedCount,
                @Resolved AS ResolvedCount,
                @Escalated AS EscalatedCount;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            await using var command =
                new SqlCommand(sql, connection, transaction);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new DataException(
                    "Reminder sync result is missing.");

            var result = new InspectionReminderSyncResult(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2));

            await reader.CloseAsync();

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                // 回滚异常需要保留在日志中；
                // 不应掩盖原始业务异常。
                System.Diagnostics.Debug.WriteLine(
                    rollbackEx);
            }

            throw;
        }
    }
}
