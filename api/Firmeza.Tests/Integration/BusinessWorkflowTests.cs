using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Firmeza.Application.Authentication;
using Firmeza.Application.Products;
using Firmeza.Application.Vehicles;
using Firmeza.Application.Rentals;
using Firmeza.Application.Sales;
using Firmeza.Application.Common;
using Firmeza.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Firmeza.Tests.Integration;

public class BusinessWorkflowTests
{
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    private static async Task<HttpClient> Admin(TestApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await Read<AuthResponse>(await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@firmeza.test", TestApiFactory.AdminPassword)));
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        return client;
    }
    private static async Task<(HttpClient Client, AuthResponse Auth)> Customer(TestApiFactory factory)
    {
        var client = factory.CreateClient();
        var uid = Guid.NewGuid().ToString("N")[..24];
        var auth = await Read<AuthResponse>(await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(uid, "Jane", "Doe", uid + "@firmeza.test", "1234567890", "Test Street", "Customer-Password-2026!")));
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        return (client, auth);
    }
    private static Task<ProductResponse> Product(HttpClient admin, int stock = 5) => CreateProduct(admin, new("Hammer", "Steel hammer", 100, stock, 0.19m));
    private static async Task<ProductResponse> CreateProduct(HttpClient admin, ProductRequest request) => await Read<ProductResponse>(await admin.PostAsJsonAsync("/api/v1/products", request));
    private static Task<HttpResponseMessage> Purchase(HttpClient client, Guid product, int quantity, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sales") { Content = JsonContent.Create(new CreateSaleRequest([new(product, quantity)])) };
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
    [DatabaseFact]
    public async Task Sales_snapshot_totals_and_restore_stock_once()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (customer, _) = await Customer(factory);
        using var buyer = customer;
        var p = await Product(admin);
        var sale = await Read<SaleResponse>(await Purchase(buyer, p.Id, 3, "purchase-1"));
        Assert.Equal(300, sale.Subtotal);
        Assert.Equal(57, sale.TaxTotal);
        Assert.Equal(357, sale.Total);
        var updated = await Read<ProductResponse>(await admin.GetAsync($"/api/v1/products/{p.Id}"));
        Assert.Equal(2, updated.Stock);
        await Read<SaleResponse>(await admin.PostAsync($"/api/v1/sales/{sale.Id}/cancel", null));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/sales/{sale.Id}/cancel", null)).StatusCode);
        Assert.Equal(5, (await Read<ProductResponse>(await admin.GetAsync($"/api/v1/products/{p.Id}"))).Stock);
    }
    [DatabaseFact]
    public async Task Simultaneous_customers_cannot_buy_the_same_last_unit()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, _) = await Customer(factory);
        var (b, _) = await Customer(factory);
        using var buyerA = a;
        using var buyerB = b;
        var p = await Product(admin, 1);
        var responses = await Task.WhenAll(Purchase(a, p.Id, 1, "a"), Purchase(b, p.Id, 1, "b"));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(0, (await Read<ProductResponse>(await admin.GetAsync($"/api/v1/products/{p.Id}"))).Stock);
    }
    [DatabaseFact]
    public async Task Idempotency_replay_does_not_duplicate_sales_and_changed_body_conflicts()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, _) = await Customer(factory);
        using var buyer = a;
        var p = await Product(admin);
        var responses = await Task.WhenAll(Purchase(a, p.Id, 1, "same"), Purchase(a, p.Id, 1, "same"));
        var first = await Read<SaleResponse>(responses[0]);
        var second = await Read<SaleResponse>(responses[1]);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await Purchase(a, p.Id, 2, "same")).StatusCode);
        Assert.Equal(4, (await Read<ProductResponse>(await admin.GetAsync($"/api/v1/products/{p.Id}"))).Stock);
    }
    [DatabaseFact]
    public async Task Invalid_sale_line_rolls_back_stock_and_outbox()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, _) = await Customer(factory);
        using var buyer = a;
        var p = await Product(admin);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sales") { Content = JsonContent.Create(new CreateSaleRequest([new(p.Id, 1), new(Guid.NewGuid(), 1)])) };
        request.Headers.Add("Idempotency-Key", "rollback");
        Assert.Equal(HttpStatusCode.NotFound, (await a.SendAsync(request)).StatusCode);
        Assert.Equal(5, (await Read<ProductResponse>(await admin.GetAsync($"/api/v1/products/{p.Id}"))).Stock);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
        Assert.Equal(0, await db.Sales.CountAsync());
        Assert.Equal(0, await db.OutboxMessages.CountAsync());
    }
    [DatabaseFact]
    public async Task Customer_cannot_read_another_customers_sales_or_manage_inventory()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, _) = await Customer(factory);
        var (b, _) = await Customer(factory);
        using var buyer = a;
        using var other = b;
        var p = await Product(admin);
        var sale = await Read<SaleResponse>(await Purchase(a, p.Id, 1, "owned"));
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/v1/sales/{sale.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.PostAsJsonAsync("/api/v1/products", new ProductRequest("Test", "", 1, 1, 0))).StatusCode);
        Assert.Empty((await Read<PagedResult<SaleResponse>>(await b.GetAsync("/api/v1/sales"))).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/v1/sales/{sale.Id}/receipt")).StatusCode);
    }
    [DatabaseFact]
    public async Task Deactivated_customer_cannot_use_an_existing_token()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, auth) = await Customer(factory);
        using var buyer = a;
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/customers/{auth.CustomerId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await a.GetAsync("/api/v1/sales")).StatusCode);
    }
    [DatabaseFact]
    public async Task Locked_customer_recheck_refreshes_a_previously_tracked_profile()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        using var firstScope = factory.Services.CreateScope();
        var db = firstScope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
        var customer = Firmeza.Domain.Entities.Customer.Create("987654321", "Jane", "Doe", "jane@example.test", "123456789", "Street");
        var product = Firmeza.Domain.Entities.Product.Create("Hammer", "", Firmeza.Domain.ValueObjects.Money.Create(100), 2, 0);
        db.AddRange(customer, product);
        await db.SaveChangesAsync();
        using (var secondScope = factory.Services.CreateScope())
        {
            var otherDb = secondScope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
            var other = await otherDb.Customers.SingleAsync(x => x.Id == customer.Id);
            other.Deactivate();
            await otherDb.SaveChangesAsync();
        }
        var service = firstScope.ServiceProvider.GetRequiredService<SaleService>();
        var error = await Assert.ThrowsAsync<RequestException>(() => service.CreateAsync(customer.Id, new CreateSaleRequest([new(product.Id, 1)]), "stale-profile", CancellationToken.None));
        Assert.Equal(409, error.Status);
        Assert.Equal(0, await db.Sales.CountAsync());
    }
    [DatabaseFact]
    public async Task Rentals_prevent_overlaps_and_allow_adjacent_intervals()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, _) = await Customer(factory);
        var (b, _) = await Customer(factory);
        using var buyer = a;
        using var other = b;
        var v = await Read<VehicleResponse>(await admin.PostAsJsonAsync("/api/v1/vehicles", new VehicleRequest("Truck", "Cargo", "Brand", "ABC123", 100)));
        var request = new CreateRentalRequest(v.Id, new(2026, 12, 1), new(2026, 12, 3));
        var responses = await Task.WhenAll(a.PostAsJsonAsync("/api/v1/rentals", request), b.PostAsJsonAsync("/api/v1/rentals", request));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        var rental = await Read<RentalResponse>(responses.Single(x => x.StatusCode == HttpStatusCode.Created));
        Assert.Equal(200, rental.Total);
        var adjacent = await Read<RentalResponse>(await a.PostAsJsonAsync("/api/v1/rentals", new CreateRentalRequest(v.Id, new(2026, 12, 3), new(2026, 12, 5))));
        Assert.Equal(200, adjacent.Total);
    }
    [DatabaseFact]
    public async Task Public_catalogue_cannot_reveal_inactive_products()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var p = await Product(admin);
        await admin.DeleteAsync($"/api/v1/products/{p.Id}");
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/products/{p.Id}")).StatusCode);
        Assert.Empty((await Read<PagedResult<ProductResponse>>(await anonymous.GetAsync("/api/v1/products?includeInactive=true"))).Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/v1/products?pageSize=100000")).StatusCode);
    }
    [DatabaseFact]
    public async Task Receipt_is_pending_without_exposing_a_static_file()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var (a, _) = await Customer(factory);
        using var buyer = a;
        var p = await Product(admin);
        var sale = await Read<SaleResponse>(await Purchase(a, p.Id, 1, "receipt"));
        var status = await Read<SaleDeliveryResponse>(await a.GetAsync($"/api/v1/sales/{sale.Id}"));
        Assert.Equal("Pending", status.Delivery!.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await a.GetAsync($"/api/v1/sales/{sale.Id}/receipt")).StatusCode);
    }
}
