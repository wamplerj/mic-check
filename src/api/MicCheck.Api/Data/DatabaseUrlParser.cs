namespace MicCheck.Api.Data;

public static class DatabaseUrlParser
{
    /// <summary>
    /// Converts a DATABASE_URL in postgresql://user:password@host:port/dbname format
    /// to an Npgsql connection string.
    /// </summary>
    public static string ToNpgsqlConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = userInfo.Length > 0 ? userInfo[0] : string.Empty;
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
}
