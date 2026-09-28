using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly string _connectionString;

    public VehicleRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }

    public async Task<List<Vehicle>> GetAllAsync()
    {
        List<Vehicle> vehicles = new();

        const string sql = """
            SELECT
                Id,
                PlateNumber,
                Model,
                Status,
                CreateTime
            FROM dbo.Vehicles
            ORDER BY Id;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            vehicles.Add(MapVehicle(reader));
        }

        return vehicles;
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                Id,
                PlateNumber,
                Model,
                Status,
                CreateTime
            FROM dbo.Vehicles
            WHERE Id = @Id;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapVehicle(reader);
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        const string sql = """
            INSERT INTO dbo.Vehicles
            (
                PlateNumber,
                Model,
                Status
            )
            OUTPUT
                INSERTED.Id,
                INSERTED.PlateNumber,
                INSERTED.Model,
                INSERTED.Status,
                INSERTED.CreateTime
            VALUES
            (
                @PlateNumber,
                @Model,
                @Status
            );
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@PlateNumber",
            vehicle.PlateNumber);

        command.Parameters.AddWithValue(
            "@Model",
            vehicle.Model);

        command.Parameters.AddWithValue(
            "@Status",
            vehicle.Status);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        return MapVehicle(reader);
    }

    public async Task<bool> UpdateAsync(Vehicle vehicle)
    {
        const string sql = """
            UPDATE dbo.Vehicles
            SET
                PlateNumber = @PlateNumber,
                Model = @Model,
                Status = @Status
            WHERE Id = @Id;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue("@Id", vehicle.Id);
        command.Parameters.AddWithValue(
            "@PlateNumber",
            vehicle.PlateNumber);
        command.Parameters.AddWithValue(
            "@Model",
            vehicle.Model);
        command.Parameters.AddWithValue(
            "@Status",
            vehicle.Status);

        int rowsAffected =
            await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = """
            DELETE FROM dbo.Vehicles
            WHERE Id = @Id;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        int rowsAffected =
            await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    private static Vehicle MapVehicle(SqlDataReader reader)
    {
        return new Vehicle
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            PlateNumber = reader.GetString(
                reader.GetOrdinal("PlateNumber")),

            Model = reader.GetString(
                reader.GetOrdinal("Model")),

            Status = reader.GetString(
                reader.GetOrdinal("Status")),

            CreateTime = reader.GetDateTime(
                reader.GetOrdinal("CreateTime"))
        };
    }

    public async Task<bool> PlateNumberExistsAsync(
        string plateNumber,
        int? excludeId = null)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.Vehicles
            WHERE PlateNumber = @PlateNumber
            AND (@ExcludeId IS NULL OR Id <> @ExcludeId);
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@PlateNumber",
            plateNumber);

        command.Parameters.AddWithValue(
            "@ExcludeId",
            (object?)excludeId ?? DBNull.Value);

        object? result =
            await command.ExecuteScalarAsync();

        int count = Convert.ToInt32(result);

        return count > 0;
    }


    
}