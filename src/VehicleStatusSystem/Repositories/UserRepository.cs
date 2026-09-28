using Microsoft.Data.SqlClient;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = """
            SELECT
                Id,
                Username,
                PasswordHash,
                Role,
                CreatedAt
            FROM dbo.Users
            WHERE Username = @Username;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@Username",
            username);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<bool> UsernameExistsAsync(
        string username)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.Users
            WHERE Username = @Username;
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@Username",
            username);

        object? result =
            await command.ExecuteScalarAsync();

        int count = Convert.ToInt32(result);

        return count > 0;
    }

    public async Task<User> AddAsync(User user)
    {
        const string sql = """
            INSERT INTO dbo.Users
            (
                Username,
                PasswordHash,
                Role
            )
            OUTPUT
                INSERTED.Id,
                INSERTED.Username,
                INSERTED.PasswordHash,
                INSERTED.Role,
                INSERTED.CreatedAt
            VALUES
            (
                @Username,
                @PasswordHash,
                @Role
            );
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.AddWithValue(
            "@Username",
            user.Username);

        command.Parameters.AddWithValue(
            "@PasswordHash",
            user.PasswordHash);

        command.Parameters.AddWithValue(
            "@Role",
            user.Role);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        return MapUser(reader);
    }

    private static User MapUser(SqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            Username = reader.GetString(
                reader.GetOrdinal("Username")),

            PasswordHash = reader.GetString(
                reader.GetOrdinal("PasswordHash")),

            Role = reader.GetString(
                reader.GetOrdinal("Role")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt"))
        };
    }
}