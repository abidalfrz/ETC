using Etc.Desktop.Infrastructure;
using Microsoft.Data.SqlClient;
namespace Etc.IntegrationTests;
[CollectionDefinition("SQL", DisableParallelization = true)]
public sealed class SqlCollection : ICollectionFixture<DatabaseFixture>;
public sealed class DatabaseFixture : IAsyncLifetime
{
    public SqlConnectionFactory Factory { get; private set; } = null!;
    public static void EnsureTestDatabase(string name)
    {
        if (name != "ETC_Test" && !name.StartsWith("ETC_Test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Reset hanya untuk database ETC_Test.");
    }
    public async Task InitializeAsync()
    {
        var value = Environment.GetEnvironmentVariable("ETC_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("ETC_TEST_CONNECTION_STRING diperlukan; test SQL tidak dilewati.");
        var b = new SqlConnectionStringBuilder(value);
        EnsureTestDatabase(b.InitialCatalog);
        var name = b.InitialCatalog;
        b.InitialCatalog = "master";
        await using var master = new SqlConnection(b.ConnectionString);
        await master.OpenAsync();
        await using var create = new SqlCommand($"IF DB_ID(@name) IS NULL CREATE DATABASE [{name.Replace("]", "]]", StringComparison.Ordinal)}]", master);
        create.Parameters.AddWithValue("@name", name);
        await create.ExecuteNonQueryAsync();
        Factory = new SqlConnectionFactory(value);
        await new DatabaseInitializer(Factory).InitializeAsync();
    }
    public async Task<object?> ScalarAsync(string sql)
    {
        await using var c = await Factory.OpenAsync();
        await using var cmd = new SqlCommand(sql, c);
        return await cmd.ExecuteScalarAsync();
    }
    public async Task ExecuteAsync(string sql)
    {
        await using var c = await Factory.OpenAsync();
        await using var cmd = new SqlCommand(sql, c);
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task ResetAsync()
    {
        EnsureTestDatabase(Factory.DatabaseName);
        await ExecuteAsync("DELETE MentorSkills; DELETE Mentors; DELETE Bookmarks; DELETE Applications; DELETE PositionSkills; DELETE Positions; DELETE Recruitments; DELETE StudentSkills; DELETE Students;");
    }
    public Task DisposeAsync() => Task.CompletedTask;
}
