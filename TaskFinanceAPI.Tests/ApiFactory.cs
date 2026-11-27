using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFinanceAPI.Data;

namespace TaskFinanceAPI.Tests;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string? sqlServer = Environment.GetEnvironmentVariable("TASKFINANCE_TEST_SQLSERVER");
    private readonly string databaseName = $"TaskFinanceTest_{Guid.NewGuid():N}";
    private readonly SqliteConnection sqlite = new("Data Source=:memory:;Foreign Keys=True");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                if (string.IsNullOrWhiteSpace(sqlServer))
                {
                    options.UseSqlite(sqlite);
                }
                else
                {
                    var connection = new SqlConnectionStringBuilder(sqlServer) { InitialCatalog = databaseName };
                    options.UseSqlServer(connection.ConnectionString);
                }
            });
        });
    }

    internal async Task PrepareDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;
        if (database.IsSqlServer())
        {
            await database.MigrateAsync();
        }
        else
        {
            await sqlite.OpenAsync();
            await database.EnsureCreatedAsync();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(sqlServer))
            {
                using var scope = Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            await base.DisposeAsync();
            await sqlite.DisposeAsync();
        }
    }
}
