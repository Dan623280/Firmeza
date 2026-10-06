using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Firmeza.Application.Authentication;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;
namespace Firmeza.Tests.Integration;

public class SecurityAndSchemaTests
{
    [DatabaseFact]
    public async Task Registration_cannot_select_an_administrator_role()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            documentNumber = "123456789",
            firstName = "Jane",
            lastName = "Doe",
            email = "jane@example.test",
            phone = "123456789",
            address = "Street",
            password = "Customer-Password-2026!",
            role = "Administrator"
        });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(new[] { "Customer" }, auth.Roles);
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/dashboard")).StatusCode);
    }
    [DatabaseFact]
    public async Task Expired_and_tampered_tokens_are_rejected()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
        var user = await db.Users.SingleAsync();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Test-only-signing-key-abcdefghijklmnopqrstuvwxyz-2026"));
        var jwt = new JwtSecurityToken("Firmeza", "Firmeza.Client", [new Claim(JwtRegisteredClaimNames.Sub, user.Id), new Claim("stamp", user.SecurityStamp!), new Claim(ClaimTypes.Role, "Administrator")], DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-5), new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var expired = new JwtSecurityTokenHandler().WriteToken(jwt);
        client.DefaultRequestHeaders.Authorization = new("Bearer", expired);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/customers")).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@firmeza.test", TestApiFactory.AdminPassword));
        var valid = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        var parts = valid.AccessToken.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        client.DefaultRequestHeaders.Authorization = new("Bearer", string.Join('.', parts));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/customers")).StatusCode);
    }
    [DatabaseFact]
    public async Task Invalid_registration_rolls_back_identity_and_profile()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("123456789", "Jane", "Doe", "jane@example.test", "123456789", "Street", "aaaaaaaaaaaa"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
        Assert.Equal(0, await db.Customers.CountAsync());
        Assert.Equal(1, await db.Users.CountAsync());
    }
    [DatabaseFact]
    public async Task Database_constraints_prevent_negative_stock_and_historical_deletes()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
        var customer = Customer.Create("123456789", "Jane", "Doe", "jane@example.test", "123456789", "Street");
        var product = Product.Create("Hammer", "", Money.Create(100), 1, 0);
        var sale = Sale.Create(customer.Id, "SCHEMA-1", [SaleItem.Create(product, 1)]);
        db.AddRange(customer, product, sale);
        await db.SaveChangesAsync();
        var negative = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Products\" SET \"Stock\" = -1 WHERE \"Id\" = {product.Id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, negative.SqlState);
        var deletion = await Assert.ThrowsAsync<PostgresException>(() => db.Products.Where(x => x.Id == product.Id).ExecuteDeleteAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, deletion.SqlState);
        var duplicate = Customer.Create("123456789", "John", "Doe", "john@example.test", "123456789", "Street");
        db.Add(duplicate);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)error.InnerException!).SqlState);
    }
    [DatabaseFact]
    public async Task Oversized_product_name_is_rejected_without_a_write()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@firmeza.test", TestApiFactory.AdminPassword));
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = new string('A', 151),
            description = "",
            price = 100,
            stock = 1,
            taxRate = 0
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<FirmezaDbContext>().Products.CountAsync());
    }
}
