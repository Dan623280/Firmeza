using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Application.Products;
using OfficeOpenXml;
using Xunit;
namespace Firmeza.Tests.Integration;

public class DataExchangeTests
{
    private static async Task<HttpClient> Admin(TestApiFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@firmeza.test", TestApiFactory.AdminPassword));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);
        return client;
    }
    private static byte[] Workbook(params string[][] rows)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Firmeza automated learning tests");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Records");
        var headers = new[] { "Type", "Reference", "Id", "Name", "Description", "Price", "Stock", "TaxRate", "DocumentNumber", "FirstName", "LastName", "Email", "Phone", "Address", "CustomerReference", "ProductReference", "Quantity", "IdempotencyKey" };
        for (var c = 0; c < headers.Length; c++)
            sheet.Cells[1, c + 1].Value = headers[c];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                sheet.Cells[r + 2, c + 1].Value = rows[r][c];
        return package.GetAsByteArray();
    }
    private static Task<HttpResponseMessage> Upload(HttpClient client, byte[] bytes)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(file, "file", "records.xlsx");
        return client.PostAsync("/api/v1/data/import", form);
    }
    [DatabaseFact]
    public async Task Mixed_excel_rows_normalize_to_products_customers_and_sales()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var bytes = Workbook(["Product", "p1", "", "Hammer", "Steel", "100", "5", "0.19"], ["Customer", "c1", "", "", "", "", "", "", "123456", "Jane", "Doe", "jane@example.test", "123456789", "Street"], ["Sale", "s1", "", "", "", "", "", "", "", "", "", "", "", "", "c1", "p1", "2", "import-sale-1"]);
        var response = await Upload(admin, bytes);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(3, result.GetProperty("appliedRows").GetInt32());
        var products = (await admin.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products"))!;
        Assert.Equal(3, products.Items.Single().Stock);
    }
    [DatabaseFact]
    public async Task Replaying_mixed_import_does_not_reset_stock_before_replayed_sale()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var productResponse = await admin.PostAsJsonAsync("/api/v1/products", new ProductRequest("Hammer", "", 100, 5, 0));
        productResponse.EnsureSuccessStatusCode();
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        var bytes = Workbook(["Product", "p1", product.Id.ToString(), "Hammer", "", "100", "5", "0"], ["Customer", "c1", "", "", "", "", "", "", "123456", "Jane", "Doe", "jane@example.test", "123456789", "Street"], ["Sale", "s1", "", "", "", "", "", "", "", "", "", "", "", "", "c1", "p1", "2", "replay-import-sale"]);
        Assert.Equal(HttpStatusCode.OK, (await Upload(admin, bytes)).StatusCode);
        Assert.Equal(3, (await admin.GetFromJsonAsync<ProductResponse>($"/api/v1/products/{product.Id}"))!.Stock);
        Assert.Equal(HttpStatusCode.OK, (await Upload(admin, bytes)).StatusCode);
        Assert.Equal(3, (await admin.GetFromJsonAsync<ProductResponse>($"/api/v1/products/{product.Id}"))!.Stock);
    }
    [DatabaseFact]
    public async Task Changed_workbook_cannot_reset_stock_around_an_existing_sale_key()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var response = await admin.PostAsJsonAsync("/api/v1/products", new ProductRequest("Hammer", "", 100, 5, 0));
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        var original = Workbook(["Product", "p1", product.Id.ToString(), "Hammer", "", "100", "5", "0"], ["Customer", "c1", "", "", "", "", "", "", "123456", "Jane", "Doe", "jane@example.test", "123456789", "Street"], ["Sale", "s1", "", "", "", "", "", "", "", "", "", "", "", "", "c1", "p1", "2", "changed-workbook-key"]);
        (await Upload(admin, original)).EnsureSuccessStatusCode();
        var changed = Workbook(["Product", "p1", product.Id.ToString(), "Hammer", "New description", "100", "5", "0"], ["Customer", "c1", "", "", "", "", "", "", "123456", "Jane", "Doe", "jane@example.test", "123456789", "Street"], ["Sale", "s1", "", "", "", "", "", "", "", "", "", "", "", "", "c1", "p1", "2", "changed-workbook-key"]);
        var report = await (await Upload(admin, changed)).Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, report.GetProperty("appliedRows").GetInt32());
        Assert.NotEmpty(report.GetProperty("errors").EnumerateArray());
        Assert.Equal(3, (await admin.GetFromJsonAsync<ProductResponse>($"/api/v1/products/{product.Id}"))!.Stock);
    }
    [DatabaseFact]
    public async Task Invalid_excel_reports_rows_without_partially_writing()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var response = await Upload(admin, Workbook(["Product", "p1", "", "Valid", "", "100", "5", "0"], ["Product", "p2", "", "Bad", "", "-1", "2", "0"]));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, result.GetProperty("appliedRows").GetInt32());
        Assert.NotEmpty(result.GetProperty("errors").EnumerateArray());
        Assert.Empty((await admin.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products"))!.Items);
    }
    [DatabaseFact]
    public async Task Malformed_workbook_is_a_validation_error()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(admin, [1, 2, 3])).StatusCode);
    }
    [DatabaseFact]
    public async Task Corrupt_workbook_crc_is_a_validation_error()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        var bytes = Workbook(["Product", "p1", "", "Hammer", "", "100", "5", "0"]);
        for (var i = 0; i < bytes.Length - 46; i++)
        {
            if (BitConverter.ToUInt32(bytes, i) == 0x02014b50)
            {
                bytes[i + 16] ^= 0xFF;
                var local = (int)BitConverter.ToUInt32(bytes, i + 42);
                bytes[local + 14] ^= 0xFF;
                break;
            }
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(admin, bytes)).StatusCode);
    }
    [DatabaseFact]
    public async Task Forged_workbook_size_metadata_is_rejected_before_excel_parsing()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        using var buffer = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("xl/oversized.xml", System.IO.Compression.CompressionLevel.SmallestSize);
            using var stream = entry.Open();
            var chunk = new byte[1024 * 1024];
            for (var i = 0; i < 51; i++)
                stream.Write(chunk);
        }
        var bytes = buffer.ToArray();
        for (var i = 0; i < bytes.Length - 46; i++)
            if (BitConverter.ToUInt32(bytes, i) == 0x02014b50)
            {
                Array.Clear(bytes, i + 24, 4);
                break;
            }
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(admin, bytes)).StatusCode);
    }
    [DatabaseFact]
    public async Task Sales_export_preserves_one_hundred_long_named_items_in_detail_rows()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        using (var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(factory.Services))
        {
            var db = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Firmeza.Infrastructure.Persistence.FirmezaDbContext>(scope.ServiceProvider);
            var customer = Firmeza.Domain.Entities.Customer.Create("222222", "Jane", "Doe", "jane@example.test", "123456789", "Street");
            var products = Enumerable.Range(0, 100).Select(i => Firmeza.Domain.Entities.Product.Create(i.ToString("D3") + new string('A', 147), "", Firmeza.Domain.ValueObjects.Money.Create(100), 1, 0)).ToArray();
            var sale = Firmeza.Domain.Entities.Sale.Create(customer.Id, "LONG-ITEMS", products.Select(p => Firmeza.Domain.Entities.SaleItem.Create(p, 1)));
            db.Add(customer);
            db.AddRange(products);
            db.Add(sale);
            await db.SaveChangesAsync();
        }
        var response = await admin.GetAsync("/api/v1/data/export/sales?format=xlsx");
        response.EnsureSuccessStatusCode();
        using var package = new ExcelPackage(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        var details = package.Workbook.Worksheets["SaleItems"];
        Assert.NotNull(details);
        Assert.Equal(101, details.Dimension.End.Row);
        var nameColumn = Enumerable.Range(1, details.Dimension.End.Column).Single(c => details.Cells[1, c].Text == "productName");
        Assert.Equal(150, details.Cells[101, nameColumn].Text.Length);
    }
    [DatabaseFact]
    public async Task Export_preserves_formula_like_names_as_literal_text()
    {
        await using var factory = new TestApiFactory();
        using var admin = await Admin(factory);
        (await admin.PostAsJsonAsync("/api/v1/products", new ProductRequest("=HYPERLINK(\"https://example.test\")", "", 100, 2, 0))).EnsureSuccessStatusCode();
        var response = await admin.GetAsync("/api/v1/data/export/products?format=xlsx");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var package = new ExcelPackage(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        var sheet = package.Workbook.Worksheets[0];
        var nameColumn = Enumerable.Range(1, sheet.Dimension.End.Column).Single(c => sheet.Cells[1, c].Text == "name");
        Assert.Empty(sheet.Cells[2, nameColumn].Formula);
        Assert.StartsWith("=HYPERLINK", sheet.Cells[2, nameColumn].Text);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/data/export/products?format=pdf")).StatusCode);
    }
}
