using System.Data;
using System.Data.SqlClient;

namespace Movies.Application.Database;

public class MsSqlDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MsSqlDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}