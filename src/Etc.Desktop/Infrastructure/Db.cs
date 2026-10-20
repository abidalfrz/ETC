using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;
namespace Etc.Desktop.Infrastructure;
internal static class Db
{
    public static SqlCommand Command(SqlConnection connection, SqlTransaction? transaction, string sql, params (string Name, object? Value)[] values)
    {
        var command=new SqlCommand(sql,connection,transaction);
        foreach(var (name,value) in values)
            command.Parameters.AddWithValue("@"+name, value is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : value ?? DBNull.Value);
        return command;
    }
    public static async Task<T?> JsonAsync<T>(SqlCommand command,CancellationToken ct)
    {
        var text=new StringBuilder();await using var r=await command.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))if(!r.IsDBNull(0))text.Append(r.GetString(0));
        return text.Length==0?default:JsonSerializer.Deserialize<T>(text.ToString());
    }
}
