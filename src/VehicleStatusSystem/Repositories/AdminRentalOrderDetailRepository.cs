
using System.Data;
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Repositories;

public class AdminRentalOrderDetailRepository
    : IAdminRentalOrderDetailRepository
{
    private readonly string _connectionString;

    public AdminRentalOrderDetailRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");
    }

    public async Task<AdminRentalOrderDetailDto?> GetByIdAsync(
        int orderId)
    {
        const string sql = """
            -- 结果集 1：订单
            SELECT
                o.Id AS OrderId,
                o.CustomerId,
                u.Username AS CustomerName,
                o.Status AS OrderStatus,
                o.TotalAmount,
                o.CreatedAt,
                o.ExpiresAt
            FROM dbo.RentalOrders o
            INNER JOIN dbo.Users u
                ON u.Id = o.CustomerId
            WHERE o.Id = @OrderId;

            -- 结果集 2：订单车辆与履约信息
            SELECT
                i.Id AS ItemId,
                i.OrderId,
                i.VehicleId,
                v.PlateNumber,
                vm.ModelName,
                i.StartAt,
                i.EndAt,
                i.ActualReturnedAt,
                i.DailyRate,
                i.RentalDays,
                i.Subtotal,
                i.Status AS ItemStatus,
                v.OperationStatus,

                vi.Id AS InspectionId,
                vi.Status AS InspectionStatus,
                vi.ReturnedAt,
                vi.InspectionDueAt,
                vi.InspectedAt,
                vi.ResultNotes,

                m.Id AS MaintenanceId,
                m.Status AS MaintenanceStatus,
                m.Priority,
                m.RepairNotes,
                m.RepairCost,
                m.AssignedTo,
                m.VerifiedBy,
                m.VerifiedAt,
                m.VerificationNotes

            FROM dbo.RentalOrderItems i
            INNER JOIN dbo.Vehicles v
                ON v.Id = i.VehicleId
            INNER JOIN dbo.VehicleModels vm
                ON vm.Id = v.VehicleModelId
            LEFT JOIN dbo.VehicleInspections vi
                ON vi.RentalOrderItemId = i.Id
            LEFT JOIN dbo.VehicleMaintenances m
                ON m.InspectionId = vi.Id
            WHERE i.OrderId = @OrderId
            ORDER BY i.Id;

            -- 结果集 3：支付信息
            SELECT
                p.Id AS PaymentId,
                p.PaymentNo,
                p.Amount,
                p.Method,
                p.Status AS PaymentStatus,
                p.CreatedAt,
                p.PaidAt
            FROM dbo.Payments p
            WHERE p.OrderId = @OrderId
            ORDER BY p.CreatedAt DESC, p.Id DESC;

            -- 结果集 4：退款记录
            SELECT
                r.Id AS RefundId,
                r.RefundNo,
                r.OrderId,
                r.OrderItemId,
                r.PaymentId,
                r.Amount,
                r.Reason,
                r.Status,
                r.CreatedAt,
                r.RefundedAt
            FROM dbo.Refunds r
            WHERE r.OrderId = @OrderId
            ORDER BY r.CreatedAt DESC, r.Id DESC;

            """;




        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@OrderId", SqlDbType.Int).Value = orderId;

        await using var reader =
            await command.ExecuteReaderAsync();

        // 辅助函数：读取可能为 NULL 的字段（时间、整形、小数、字符串）
        static DateTime? NullableDate(
            SqlDataReader r, string column)
        {
            int index = r.GetOrdinal(column);
            return r.IsDBNull(index)
                ? null : r.GetDateTime(index);
        }

        static int? NullableInt(
            SqlDataReader r, string column)
        {
            int index = r.GetOrdinal(column);
            return r.IsDBNull(index)
                ? null : r.GetInt32(index);
        }

        static decimal? NullableDecimal(
            SqlDataReader r, string column)
        {
            int index = r.GetOrdinal(column);
            return r.IsDBNull(index)
                ? null : r.GetDecimal(index);
        }

        static string? NullableString(
            SqlDataReader r, string column)
        {
            int index = r.GetOrdinal(column);
            return r.IsDBNull(index)
                ? null : r.GetString(index);
        }

        // ① 读取订单主表
        if (!await reader.ReadAsync())
            return null;

        var order = new AdminRentalOrderDetailDto
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
            ExpiresAt = NullableDate(reader, "ExpiresAt")
        };

        // ② 切换到车辆明细
        if (!await reader.NextResultAsync())
            throw new DataException(
                "Order items result set is missing.");

        while (await reader.ReadAsync())
        {
            var item = new AdminOrderItemDetailDto
            {
                ItemId = reader.GetInt32(
                    reader.GetOrdinal("ItemId")),
                VehicleId = reader.GetInt32(
                    reader.GetOrdinal("VehicleId")),
                PlateNumber = reader.GetString(
                    reader.GetOrdinal("PlateNumber")),
                ModelName = reader.GetString(
                    reader.GetOrdinal("ModelName")),

                StartAt = reader.GetDateTime(
                    reader.GetOrdinal("StartAt")),
                EndAt = reader.GetDateTime(
                    reader.GetOrdinal("EndAt")),
                ActualReturnedAt =
                    NullableDate(reader, "ActualReturnedAt"),

                DailyRate = reader.GetDecimal(
                    reader.GetOrdinal("DailyRate")),
                RentalDays = reader.GetInt32(
                    reader.GetOrdinal("RentalDays")),
                Subtotal = reader.GetDecimal(
                    reader.GetOrdinal("Subtotal")),

                ItemStatus = reader.GetString(
                    reader.GetOrdinal("ItemStatus")),
                OperationStatus = reader.GetString(
                    reader.GetOrdinal("OperationStatus"))
            };

            // 有验车记录才创建 Inspection DTO
            if (!reader.IsDBNull(
                reader.GetOrdinal("InspectionId")))
            {
                item.Inspection = new AdminOrderInspectionDto
                {
                    InspectionId = reader.GetInt32(
                        reader.GetOrdinal("InspectionId")),
                    Status = reader.GetString(
                        reader.GetOrdinal("InspectionStatus")),
                    ReturnedAt = reader.GetDateTime(
                        reader.GetOrdinal("ReturnedAt")),
                    InspectionDueAt = reader.GetDateTime(
                        reader.GetOrdinal("InspectionDueAt")),
                    InspectedAt =
                        NullableDate(reader, "InspectedAt"),
                    ResultNotes =
                        NullableString(reader, "ResultNotes")
                };
            }

            // 有维修记录才创建 Maintenance DTO
            if (!reader.IsDBNull(
                reader.GetOrdinal("MaintenanceId")))
            {
                item.Maintenance = new AdminOrderMaintenanceDto
                {
                    MaintenanceId = reader.GetInt32(
                        reader.GetOrdinal("MaintenanceId")),
                    Status = reader.GetString(
                        reader.GetOrdinal("MaintenanceStatus")),
                    Priority = reader.GetString(
                        reader.GetOrdinal("Priority")),
                    RepairNotes =
                        NullableString(reader, "RepairNotes"),
                    RepairCost =
                        NullableDecimal(reader, "RepairCost"),
                    AssignedTo =
                        NullableInt(reader, "AssignedTo"),
                    VerifiedBy =
                        NullableInt(reader, "VerifiedBy"),
                    VerifiedAt =
                        NullableDate(reader, "VerifiedAt"),
                    VerificationNotes =
                        NullableString(reader, "VerificationNotes")
                };
            }

            order.Items.Add(item);
        }

        // ③ 切换到支付信息
        if (!await reader.NextResultAsync())
            throw new DataException(
                "Payments result set is missing.");

        while (await reader.ReadAsync())
        {
            order.Payments.Add(new AdminOrderPaymentDto
            {
                PaymentId = reader.GetInt64(
                    reader.GetOrdinal("PaymentId")),
                PaymentNo = reader.GetGuid(
                    reader.GetOrdinal("PaymentNo")).ToString(),
                Amount = reader.GetDecimal(
                    reader.GetOrdinal("Amount")),
                Method = reader.GetString(
                    reader.GetOrdinal("Method")),
                PaymentStatus = reader.GetString(
                    reader.GetOrdinal("PaymentStatus")),
                CreatedAt = reader.GetDateTime(
                    reader.GetOrdinal("CreatedAt")),
                PaidAt = NullableDate(reader, "PaidAt")
            });
        }

        // ④ 切换到退款记录
        if (!await reader.NextResultAsync())
        {
            throw new DataException(
                "Refunds result set is missing.");
        }

        while (await reader.ReadAsync())
        {
            order.Refunds.Add(new AdminOrderRefundDto
            {
                RefundId = reader.GetInt64(
                    reader.GetOrdinal("RefundId")),

                RefundNo = reader.GetGuid(
                    reader.GetOrdinal("RefundNo")).ToString(),

                OrderId = reader.GetInt32(
                    reader.GetOrdinal("OrderId")),

                OrderItemId = NullableInt(
                    reader, "OrderItemId"),

                PaymentId = reader.GetInt64(
                    reader.GetOrdinal("PaymentId")),

                Amount = reader.GetDecimal(
                    reader.GetOrdinal("Amount")),

                Reason = reader.GetString(
                    reader.GetOrdinal("Reason")),

                Status = reader.GetString(
                    reader.GetOrdinal("Status")),

                CreatedAt = reader.GetDateTime(
                    reader.GetOrdinal("CreatedAt")),

                RefundedAt = NullableDate(
                    reader, "RefundedAt")
            });
        }

        return order;
    }
}
