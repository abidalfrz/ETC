using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Infrastructure;
public sealed class SqlConnectionFactory(string connectionString)
{
    public string DatabaseName => new SqlConnectionStringBuilder(connectionString).InitialCatalog;
    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(ct); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
