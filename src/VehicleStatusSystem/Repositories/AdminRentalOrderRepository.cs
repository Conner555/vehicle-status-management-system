
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Repositories;

public class AdminRentalOrderRepository
    : IAdminRentalOrderRepository
{
    private readonly string _connectionString;

    public AdminRentalOrderRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<AdminRentalOrderPageDto> GetPagedAsync(
        AdminRentalOrderQueryDto query)
    {
        long offset = checked(
            ((long)query.Page - 1) * query.PageSize);

        const string sql = """
            SELECT COUNT_BIG(*)
            FROM dbo.RentalOrders AS o
            WHERE
                (@Status IS NULL OR o.Status = @Status)
                AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)
                AND (@CreatedFrom IS NULL OR o.CreatedAt >= @CreatedFrom)
                AND (@CreatedTo IS NULL OR o.CreatedAt < @CreatedTo);

            SELECT
                o.Id AS OrderId,
                o.CustomerId,
                u.Username AS CustomerName,
                o.Status AS OrderStatus,
                o.TotalAmount,
                o.CreatedAt,
                o.ExpiresAt,

                (
                    SELECT COUNT(*)
                    FROM dbo.RentalOrderItems AS i
                    WHERE i.OrderId = o.Id
                ) AS VehicleCount,

                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM dbo.Payments AS p
                        WHERE p.OrderId = o.Id
                          AND p.Status = 'Succeeded'
                    )
                    THEN 'Succeeded'
                    ELSE NULL
                END AS PaymentStatus

            FROM dbo.RentalOrders AS o
            INNER JOIN dbo.Users AS u
                ON u.Id = o.CustomerId
            WHERE
                (@Status IS NULL OR o.Status = @Status)
                AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)
                AND (@CreatedFrom IS NULL OR o.CreatedAt >= @CreatedFrom)
                AND (@CreatedTo IS NULL OR o.CreatedAt < @CreatedTo)

            ORDER BY o.CreatedAt DESC, o.Id DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@Status", SqlDbType.NVarChar, 30).Value =
            (object?)query.Status ?? DBNull.Value;

        command.Parameters.Add(
            "@CustomerId", SqlDbType.Int).Value =
            (object?)query.CustomerId ?? DBNull.Value;

        command.Parameters.Add(
            "@CreatedFrom", SqlDbType.DateTime2).Value =
            (object?)query.CreatedFrom ?? DBNull.Value;

        command.Parameters.Add(
            "@CreatedTo", SqlDbType.DateTime2).Value =
            (object?)query.CreatedTo ?? DBNull.Value;

        command.Parameters.Add(
            "@Offset", SqlDbType.BigInt).Value = offset;

        command.Parameters.Add(
            "@PageSize", SqlDbType.Int).Value = query.PageSize;

        var result = new AdminRentalOrderPageDto
        {
            Page = query.Page,
            PageSize = query.PageSize
        };

        await using var reader =
            await command.ExecuteReaderAsync();

        // 第一个结果集：TotalCount
        if (await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt64(0);
        }

        // 切换到第二个结果集：订单列表
        if (!await reader.NextResultAsync())
        {
            throw new DataException(
                "Order query result set is missing.");
        }

        while (await reader.ReadAsync())
        {
            result.Items.Add(new AdminRentalOrderDto
            {
                OrderId = reader.GetInt32(
                    reader.GetOrdinal("OrderId")),

                CustomerId = reader.GetInt32(
                    reader.GetOrdinal("CustomerId")),

                CustomerName = reader.GetString(
                    reader.GetOrdinal("CustomerName")),

                OrderStatus = reader.GetString(
                    reader.GetOrdinal("OrderStatus")),

                TotalAmount = reader.GetDecimal(
                    reader.GetOrdinal("TotalAmount")),

                CreatedAt = reader.GetDateTime(
                    reader.GetOrdinal("CreatedAt")),

                ExpiresAt = reader.IsDBNull(
                    reader.GetOrdinal("ExpiresAt"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("ExpiresAt")),

                VehicleCount = reader.GetInt32(
                    reader.GetOrdinal("VehicleCount")),

                PaymentStatus = reader.IsDBNull(
                    reader.GetOrdinal("PaymentStatus"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("PaymentStatus"))
            });
        }

        return result;
    }
}
