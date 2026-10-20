using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Infrastructure;
public sealed class DatabaseInitializer(SqlConnectionFactory connections)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await using (var cmd = new SqlCommand("EXEC sp_getapplock @Resource='ETC_schema', @LockMode='Exclusive', @LockOwner='Transaction';", connection, tx))
            await cmd.ExecuteNonQueryAsync(ct);
        var assembly = typeof(DatabaseInitializer).Assembly;
        foreach (var file in new[] { "001-schema.sql", "002-catalog.sql", "003-mentor-directory.sql" })
        {
            var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith(file, StringComparison.Ordinal));
            await using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            await using var cmd = new SqlCommand(await reader.ReadToEndAsync(ct), connection, tx);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
