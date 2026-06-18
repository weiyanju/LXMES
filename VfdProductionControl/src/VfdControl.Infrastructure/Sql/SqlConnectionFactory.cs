using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace VfdControl.Infrastructure.Sql;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("SQL Server connection string is required.", nameof(connectionString))
            : connectionString;
    }

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("VfdProductionControl")
            ?? configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("SQL Server connection string is not configured.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
