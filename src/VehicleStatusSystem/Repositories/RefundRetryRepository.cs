
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Repositories;

public sealed class RefundRetryRepository : IRefundRetryRepository
{
    private readonly string _connectionString;

    public RefundRetryRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<long> RecordAsync(
        Guid operationKey,
        int orderId,
        int? orderItemId,
        string reason,
        decimal amount,
        string error,
        CancellationToken cancellationToken)
    {
        if (operationKey == Guid.Empty ||
            orderId <= 0 || amount <= 0 ||
            (orderItemId.HasValue && orderItemId.Value <= 0) ||
            reason is not ("Cancellation" or "EarlyReturn"))
        {
            throw new ArgumentException(
                "Invalid refund retry task.");
        }

        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException(
                "Error description is required.",
                nameof(error));

        const string sql = """
            DECLARE @TaskId BIGINT;

            SELECT @TaskId = Id
            FROM dbo.RefundRetryTasks WITH (UPDLOCK, HOLDLOCK)
            WHERE OperationKey = @OperationKey;

            IF @TaskId IS NULL
            BEGIN
                INSERT INTO dbo.RefundRetryTasks
                (
                    OperationKey,
                    OrderId,
                    OrderItemId,
                    Reason,
                    Amount,
                    LastError
                )
                VALUES
                (
                    @OperationKey,
                    @OrderId,
                    @OrderItemId,
                    @Reason,
                    @Amount,
                    @Error
                );

                SET @TaskId = CONVERT(BIGINT, SCOPE_IDENTITY());
            END;

            SELECT @TaskId;
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

            command.Parameters.Add(
                "@OperationKey", SqlDbType.UniqueIdentifier)
                .Value = operationKey;

            command.Parameters.Add(
                "@OrderId", SqlDbType.Int)
                .Value = orderId;

            command.Parameters.Add(
                "@OrderItemId", SqlDbType.Int)
                .Value = (object?)orderItemId ?? DBNull.Value;

            command.Parameters.Add(
                "@Reason", SqlDbType.NVarChar, 30)
                .Value = reason;

            command.Parameters.Add(
                "@Amount", SqlDbType.Decimal)
                .Value = amount;

            command.Parameters["@Amount"].Precision = 18;
            command.Parameters["@Amount"].Scale = 2;

            command.Parameters.Add(
                "@Error", SqlDbType.NVarChar, 1000)
                .Value = error.Length > 1000
                    ? error[..1000]
                    : error;

            long taskId = Convert.ToInt64(
                await command.ExecuteScalarAsync(cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return taskId;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);
            throw;
        }
    }

    public async Task<RefundRetryTaskDto?> GetByIdAsync(
        long taskId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id, OperationKey, OrderId, OrderItemId,
                Reason, Amount, Status, RetryCount,
                NextRetryAt, LastError
            FROM dbo.RefundRetryTasks
            WHERE Id = @TaskId;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@TaskId", SqlDbType.BigInt).Value = taskId;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        int itemIndex = reader.GetOrdinal("OrderItemId");
        int retryAtIndex = reader.GetOrdinal("NextRetryAt");
        int errorIndex = reader.GetOrdinal("LastError");

        return new RefundRetryTaskDto
        {
            Id = reader.GetInt64(0),
            OperationKey = reader.GetGuid(1),
            OrderId = reader.GetInt32(2),
            OrderItemId = reader.IsDBNull(itemIndex)
                ? null : reader.GetInt32(itemIndex),
            Reason = reader.GetString(4),
            Amount = reader.GetDecimal(5),
            Status = reader.GetString(6),
            RetryCount = reader.GetInt32(7),
            NextRetryAt = reader.IsDBNull(retryAtIndex)
                ? null : reader.GetDateTime(retryAtIndex),
            LastError = reader.IsDBNull(errorIndex)
                ? null : reader.GetString(errorIndex)
        };
    }
}
