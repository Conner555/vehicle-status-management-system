
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;
using VehicleStatusSystem.Domain;

namespace VehicleStatusSystem.Repositories;

public class EarlyReturnSettlementRepository
    : IEarlyReturnSettlementRepository
{
    private readonly string _connectionString;

    public EarlyReturnSettlementRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<EarlyReturnSettlementData?> GetPreviewDataAsync(
        int orderItemId,
        int customerId)
    {
        const string sql = """
            SELECT
                i.Id AS OrderItemId,
                i.OrderId,
                o.CustomerId,
                i.RentalDays,
                i.DailyRate,
                i.Subtotal,
                i.ActualHandedOverAt,
                i.ActualReturnedAt,

                vi.Status AS InspectionStatus,
                r.Status AS RefundStatus

            FROM dbo.RentalOrderItems AS i

            INNER JOIN dbo.RentalOrders AS o
                ON o.Id = i.OrderId

            LEFT JOIN dbo.VehicleInspections AS vi
                ON vi.RentalOrderItemId = i.Id

            LEFT JOIN dbo.Refunds AS r
                ON r.OrderItemId = i.Id
                AND r.Reason = 'EarlyReturn'

            WHERE i.Id = @OrderItemId
              AND o.CustomerId = @CustomerId;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@OrderItemId", SqlDbType.Int).Value = orderItemId;

        command.Parameters.Add(
            "@CustomerId", SqlDbType.Int).Value = customerId;

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        // 1. 提前获取可空字段的列索引
        int handoverIndex =
            reader.GetOrdinal("ActualHandedOverAt");

        int returnedIndex =
            reader.GetOrdinal("ActualReturnedAt");

        int inspectionIndex =
            reader.GetOrdinal("InspectionStatus");

        int refundIndex =
            reader.GetOrdinal("RefundStatus");

        // 2. 将数据库数据映射为 Model
        return new EarlyReturnSettlementData
        {
            OrderItemId = reader.GetInt32(
                reader.GetOrdinal("OrderItemId")),

            OrderId = reader.GetInt32(
                reader.GetOrdinal("OrderId")),

            CustomerId = reader.GetInt32(
                reader.GetOrdinal("CustomerId")),

            BookedDays = reader.GetInt32(
                reader.GetOrdinal("RentalDays")),

            DailyRate = reader.GetDecimal(
                reader.GetOrdinal("DailyRate")),

            Subtotal = reader.GetDecimal(
                reader.GetOrdinal("Subtotal")),

            ActualHandedOverAt =
                reader.IsDBNull(handoverIndex)
                    ? null
                    : reader.GetDateTime(handoverIndex),

            ActualReturnedAt =
                reader.IsDBNull(returnedIndex)
                    ? null
                    : reader.GetDateTime(returnedIndex),

            InspectionStatus =
                reader.IsDBNull(inspectionIndex)
                    ? "NotCreated"
                    : reader.GetString(inspectionIndex),

            RefundStatus =
                reader.IsDBNull(refundIndex)
                    ? null
                    : reader.GetString(refundIndex)
        };
    }
}
