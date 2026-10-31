
using Microsoft.Data.SqlClient;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Interfaces;

namespace VehicleStatusSystem.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly string _connectionString;

    public DashboardRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found."
            );
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*)
                 FROM dbo.Vehicles) AS TotalVehicles,

                (SELECT COUNT(*)
                 FROM dbo.Vehicles
                 WHERE Status = 'Active') AS ActiveVehicles,

                (SELECT COUNT(*)
                 FROM dbo.Vehicles
                 WHERE Status = 'Inactive') AS InactiveVehicles,

                (SELECT COUNT(*)
                 FROM dbo.VehicleStatusRecords) AS TotalRecords;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "Dashboard query returned no result."
            );
        }

        return new DashboardSummaryDto
        {
            TotalVehicles =
                reader.GetInt32(
                    reader.GetOrdinal("TotalVehicles")),

            ActiveVehicles =
                reader.GetInt32(
                    reader.GetOrdinal("ActiveVehicles")),

            InactiveVehicles =
                reader.GetInt32(
                    reader.GetOrdinal("InactiveVehicles")),

            TotalRecords =
                reader.GetInt32(
                    reader.GetOrdinal("TotalRecords"))
        };
    }
}
