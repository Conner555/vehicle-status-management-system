using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class VehicleStatusRepository
    : IVehicleStatusRepository
{
    private readonly string _connectionString;

    public VehicleStatusRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }

    public async Task<List<VehicleStatusRecord>>
        GetByVehicleIdAsync(int vehicleId)
    {
        List<VehicleStatusRecord> records = new();

        const string sql = """
            SELECT
                Id,
                VehicleId,
                Temperature,
                Speed,
                Battery,
                RecordTime
            FROM dbo.VehicleStatusRecords
            WHERE VehicleId = @VehicleId
            ORDER BY RecordTime DESC;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@VehicleId",
            vehicleId);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(MapRecord(reader));
        }

        return records;
    }

    public async Task<VehicleStatusRecord> AddAsync(
        VehicleStatusRecord record)
    {
        const string sql = """
            INSERT INTO dbo.VehicleStatusRecords
            (
                VehicleId,
                Temperature,
                Speed,
                Battery
            )
            OUTPUT
                INSERTED.Id,
                INSERTED.VehicleId,
                INSERTED.Temperature,
                INSERTED.Speed,
                INSERTED.Battery,
                INSERTED.RecordTime
            VALUES
            (
                @VehicleId,
                @Temperature,
                @Speed,
                @Battery
            );
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@VehicleId",
            record.VehicleId);

        command.Parameters.AddWithValue(
            "@Temperature",
            record.Temperature);

        command.Parameters.AddWithValue(
            "@Speed",
            record.Speed);

        command.Parameters.AddWithValue(
            "@Battery",
            record.Battery);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        return MapRecord(reader);
    }

    public async Task<bool> HasRecordsAsync(int vehicleId)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.VehicleStatusRecords
            WHERE VehicleId = @VehicleId;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@VehicleId",
            vehicleId);

        object? result =
            await command.ExecuteScalarAsync();

        int count = Convert.ToInt32(result);

        return count > 0;
    }

    private static VehicleStatusRecord MapRecord(
        SqlDataReader reader)
    {
        return new VehicleStatusRecord
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            VehicleId = reader.GetInt32(
                reader.GetOrdinal("VehicleId")),

            Temperature = reader.GetDecimal(
                reader.GetOrdinal("Temperature")),

            Speed = reader.GetDecimal(
                reader.GetOrdinal("Speed")),

            Battery = reader.GetDecimal(
                reader.GetOrdinal("Battery")),

            RecordTime = reader.GetDateTime(
                reader.GetOrdinal("RecordTime"))
        };
    }
}