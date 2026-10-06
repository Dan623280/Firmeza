using Firmeza.Infrastructure.Persistence;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
namespace Firmeza.Tests.Integration;

public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FIRMEZA_TEST_DATABASE")))
            Skip = "Set FIRMEZA_TEST_DATABASE to a disposable PostgreSQL instance.";
    }
}
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _adminConnection;
    private readonly string _connection;
    private readonly string _database = "firmeza_test_" + Guid.NewGuid().ToString("N");
    public const string AdminPassword = "Testing-Administrator-2026!";
    public TestApiFactory()
    {
        _adminConnection = Environment.GetEnvironmentVariable("FIRMEZA_TEST_DATABASE") ?? throw new InvalidOperationException("Set FIRMEZA_TEST_DATABASE.");
        using var connection = new NpgsqlConnection(_adminConnection);
        connection.Open();
        using var command = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", connection);
        command.ExecuteNonQuery();
        var builder = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _database };
        _connection = builder.ConnectionString;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connection);
        builder.UseSetting("Jwt:SigningKey", "Test-only-signing-key-abcdefghijklmnopqrstuvwxyz-2026");
        builder.UseSetting("BootstrapAdmin:Email", "admin@firmeza.test");
        builder.UseSetting("BootstrapAdmin:Password", AdminPassword);
        builder.UseSetting("Delivery:Enabled", "false");
        builder.UseSetting("Licenses:QuestPdf", "Community");
        builder.UseSetting("Licenses:EPPlus", "NonCommercialPersonal:Firmeza automated learning tests");
        builder.UseSetting("Receipts:Directory", Path.Combine(Path.GetTempPath(), _database, "receipts"));
        builder.ConfigureServices(services =>
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<FirmezaDbContext>().Database.Migrate();
            scope.ServiceProvider.GetRequiredService<IdentityInitializer>().InitializeAsync().GetAwaiter().GetResult();
        });
    }
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        var receipts = Path.Combine(Path.GetTempPath(), _database);
        if (Directory.Exists(receipts))
            Directory.Delete(receipts, true);
        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
