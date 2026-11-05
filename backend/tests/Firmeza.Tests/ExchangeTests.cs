using AutoMapper;
using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Firmeza.Application.Services;
using Firmeza.Domain.Entities;
using Firmeza.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using Xunit;
namespace Firmeza.Tests;

public sealed class ExchangeTests
{
    static ExchangeService Service(MemoryDb db)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Firmeza pruebas académicas");
        var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        return new(db, new CatalogService(db, mapper), new SalesService(db, mapper, new Pdf(), new Mail()));
    }
    [Fact]
    public async Task ExportAndReimportPreservesDescriptionAndInactiveState()
    {
        var db = new MemoryDb();
        var product = new Product { Name = "Cemento", Description = "Saco 50kg", Price = 100, Stock = 10, Active = false };
        db.Products.Add(product);
        var service = Service(db);
        var file = await service.ExportAsync("productos", "xlsx", default);
        product.Description = "Cambio temporal";
        using var stream = new MemoryStream(file.Content);
        var result = await service.ImportAsync(stream, default);
        Assert.Empty(result.Errors);
        Assert.Equal("Saco 50kg", product.Description);
        Assert.False(product.Active);
        Assert.Equal(1, result.Products);
    }
    [Fact]
    public async Task MixedRowCreatesCustomerProductAndRelatedSale()
    {
        var db = new MemoryDb();
        var service = Service(db);
        using var stream = Workbook(["ProductoNombre", "ProductoPrecio", "ProductoStock", "ClienteDocumento", "ClienteNombre", "ClienteApellido", "ClienteCorreo", "ClienteTelefono", "ClienteDireccion", "ClienteEdad", "VentaCantidad"], ["Cemento", 100, 10, "123456", "Ana", "Pérez", "ana@example.com", "3001234567", "Calle 1", 25, 2]);
        var result = await service.ImportAsync(stream, default);
        Assert.Empty(result.Errors);
        Assert.Equal(1, result.Sales);
        Assert.Equal(8, (await db.Products.ListAsync())[0].Stock);
        Assert.Equal((await db.Customers.ListAsync())[0].Id, db.Sales[0].CustomerId);
        Assert.Equal(238m, db.Sales[0].Total);
    }
    [Fact]
    public async Task DecimalAgeIsLoggedWithRowNumber()
    {
        var db = new MemoryDb();
        var service = Service(db);
        using var stream = Workbook(["ClienteDocumento", "ClienteNombre", "ClienteApellido", "ClienteCorreo", "ClienteTelefono", "ClienteDireccion", "ClienteEdad"], ["123456", "Ana", "Pérez", "ana@example.com", "3001234567", "Calle 1", 25.5]);
        var result = await service.ImportAsync(stream, default);
        Assert.Single(result.Errors);
        Assert.Contains("fila 2", result.Errors[0]);
        Assert.Empty(await db.Customers.ListAsync());
    }
    static MemoryStream Workbook(string[] headers, object[] values)
    {
        using var package = new ExcelPackage();
        var s = package.Workbook.Worksheets.Add("Mixta");
        for (int c = 0; c < headers.Length; c++)
        {
            s.Cells[1, c + 1].Value = headers[c];
            s.Cells[2, c + 1].Value = values[c];
        }
        return new MemoryStream(package.GetAsByteArray());
    }
    sealed class Pdf : IReceiptService
    {
        public Task<FileDto> GenerateAsync(Sale sale, CancellationToken ct) => Task.FromResult(new FileDto([1], "application/pdf", "prueba.pdf"));
    }
    sealed class Mail : IEmailService
    {
        public Task<bool> SendReceiptAsync(string email, string number, FileDto file, CancellationToken ct) => Task.FromResult(false);
    }
}
