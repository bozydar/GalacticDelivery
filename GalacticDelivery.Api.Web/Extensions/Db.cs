using GalacticDelivery.Db;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Api.Web.Extensions;

public static class Db
{
    public static void ConfigureDatabaseConnection(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=:memory:";
        builder.Services.AddScoped(_ =>
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
            command.ExecuteNonQuery();
            return connection;
        });
    }

    public static void InitializeDatabaseSchema(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<SqliteConnection>();
        using var command = connection.CreateCommand();
        command.CommandText = Schema.V1 + Schema.Seed;
        command.ExecuteNonQuery();
    }
}
