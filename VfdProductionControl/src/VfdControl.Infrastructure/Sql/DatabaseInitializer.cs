using System.Reflection;
using Dapper;

namespace VfdControl.Infrastructure.Sql;

public sealed class DatabaseInitializer
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(await LoadSchemaAsync(ct));
    }

    private static async Task<string> LoadSchemaAsync(CancellationToken ct)
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        var schemaPath = Path.Combine(assemblyLocation, "Sql", "schema.sql");
        return await File.ReadAllTextAsync(schemaPath, ct);
    }
}
