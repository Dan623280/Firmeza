using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace Firmeza.Tests.Integration;

public class ApiTests
{
    [DatabaseFact]
    public async Task Catalogue_exposes_paginated_products()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
    }
    [DatabaseFact]
    public async Task Anonymous_customer_management_is_unauthorized()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
