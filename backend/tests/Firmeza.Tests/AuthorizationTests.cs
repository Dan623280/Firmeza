using System.Net.Http.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Firmeza.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;
namespace Firmeza.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "Test-key-with-at-least-32-characters-long-for-JWT";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { { "Jwt:Key", Key }, { "Jwt:Issuer", "Firmeza" }, { "Jwt:Audience", "Firmeza.Client" } }));
        builder.ConfigureServices(s => { s.RemoveAll<IUnitOfWork>(); s.AddScoped<IUnitOfWork, MemoryDb>(); });
    }
}
public sealed class AuthorizationTests : IClassFixture<ApiFactory>
{
    readonly ApiFactory factory; public AuthorizationTests(ApiFactory factory) => this.factory = factory;
    [Theory]
    [InlineData("/api/clientes")]
    [InlineData("/api/ventas")]
    [InlineData("/api/admin/dashboard")]
    public async Task AnonymousCannotAccessProtectedEndpoints(string path)
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }
    [Theory]
    [InlineData("/api/clientes")]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/admin/exportar/productos/xlsx")]
    public async Task CustomerCannotAccessAdministrativeEndpoints(string path)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Cliente"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }
    [Fact]
    public async Task AdministratorCanReadDashboard()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrador"));
        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task InvalidProductReturnsValidationProblem()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrador"));
        var response = await client.PostAsJsonAsync("/api/productos", new
        {
            name = "",
            description = "",
            price = -1,
            stock = -1
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    static string Token(string role)
    {
        var jwt = new JwtSecurityToken("Firmeza", "Firmeza.Client", [new Claim(ClaimTypes.NameIdentifier, "buyer"), new Claim(ClaimTypes.Role, role)], expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.Key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
