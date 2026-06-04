using System.Data;
using System.Text;
using Npgsql;

namespace MicCheck.Api.Tests.Integration.Common;

public sealed class TestDatabase(string connectionString)
{
    public async Task<T> ExecuteScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);
        var result = await command.ExecuteScalarAsync();
        if (result is null || result is DBNull)
            throw new InvalidOperationException($"Scalar query returned NULL. SQL: {sql}");
        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<string> SnapshotRowsAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync();

        var output = new StringBuilder();
        output.AppendLine($"SQL: {sql}");

        if (!reader.HasRows)
        {
            output.AppendLine("(no rows)");
            return output.ToString();
        }

        var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        output.AppendLine(string.Join(" | ", columnNames));

        while (await reader.ReadAsync())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? "");
            output.AppendLine(string.Join(" | ", values));
        }

        return output.ToString();
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, (string Name, object Value)[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }
}
