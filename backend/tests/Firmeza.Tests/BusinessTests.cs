using AutoMapper;
using Firmeza.Application.DTOs;
using Firmeza.Application.Exceptions;
using Firmeza.Application.Interfaces;
using Firmeza.Application.Services;
using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace Firmeza.Tests;

public sealed class BusinessTests
{
    static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
    [Fact] public void MappingConfigurationIsValid() => Mapper().ConfigurationProvider.AssertConfigurationIsValid();
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void InvalidStockRequestIsRejected(int quantity)
    {
        var p = new Product { Name = "Cemento", Stock = 5 };
        Assert.Throws<BusinessException>(() => p.RemoveStock(quantity));
        Assert.Equal(5, p.Stock);
    }
    [Fact]
    public void InactiveProductCannotBePurchased()
    {
        var p = new Product { Active = false, Stock = 20 };
        Assert.Throws<BusinessException>(() => p.RemoveStock(1));
    }
    [Fact]
    public void SaleRoundsTax()
    {
        var sale = new Sale { Details = [new() { Subtotal = 0.03m }] };
        sale.CalculateTotals();
        Assert.Equal(.01m, sale.Tax);
        Assert.Equal(.04m, sale.Total);
    }
    [Fact]
    public async Task PurchaseUsesServerPricesAndAggregatesRepeatedProducts()
    {
        var db = new MemoryDb();
        var p = new Product { Name = "Cemento", Price = 100, Stock = 10 };
        db.Products.Add(p);
        db.Customers.Add(new Customer { UserId = "buyer", Name = "Ana" });
        var service = new SalesService(db, Mapper(), new Receipt(), new Mail());
        var sale = await service.Create(new(null, [new(p.Id, 2), new(p.Id, 1)]), "buyer", false, default);
        Assert.Equal(7, p.Stock);
        Assert.Single(sale.Details);
        Assert.Equal(357m, sale.Total);
        Assert.False(sale.EmailSent);
    }
    [Fact]
    public async Task CustomerCannotChooseAnotherCustomer()
    {
        var db = new MemoryDb();
        var own = new Customer { UserId = "buyer" };
        var other = new Customer { UserId = "other" };
        db.Customers.Add(own);
        db.Customers.Add(other);
        var p = new Product { Name = "Cemento", Price = 100, Stock = 10 };
        db.Products.Add(p);
        var service = new SalesService(db, Mapper(), new Receipt(), new Mail());
        var sale = await service.Create(new(other.Id, [new(p.Id, 1)]), "buyer", false, default);
        Assert.Equal(own.Id, sale.CustomerId);
    }
    [Fact]
    public async Task CustomerCannotReadAnotherReceipt()
    {
        var db = new MemoryDb();
        db.Sales.Add(new Sale { Customer = new Customer { UserId = "other" } });
        var service = new SalesService(db, Mapper(), new Receipt(), new Mail());
        await Assert.ThrowsAsync<NotFoundException>(() => service.Get(db.Sales[0].Id, "buyer", false, default));
    }
    [Fact]
    public async Task CancelRestoresStockOnlyOnce()
    {
        var db = new MemoryDb();
        var p = new Product { Stock = 4 };
        db.Products.Add(p);
        var s = new Sale { Customer = new Customer(), Details = [new() { ProductId = p.Id, Quantity = 2 }] };
        db.Sales.Add(s);
        var service = new SalesService(db, Mapper(), new Receipt(), new Mail());
        await service.Cancel(s.Id, default);
        Assert.Equal(6, p.Stock);
        await Assert.ThrowsAsync<BusinessException>(() => service.Cancel(s.Id, default));
        Assert.Equal(6, p.Stock);
    }
    [Fact]
    public async Task RentalRejectsOverlappingDates()
    {
        var db = new MemoryDb();
        var customer = new Customer { UserId = "buyer" };
        db.Customers.Add(customer);
        var v = new Vehicle { PricePerDay = 100 };
        db.Vehicles.Add(v);
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        db.Rentals.Add(new Rental { VehicleId = v.Id, Vehicle = v, Customer = customer, StartDate = date, EndDate = date.AddDays(3) });
        var service = new SalesService(db, Mapper(), new Receipt(), new Mail());
        await Assert.ThrowsAsync<BusinessException>(() => service.Rent(new(v.Id, date.AddDays(1), date.AddDays(2)), "buyer", false, default));
    }
    [Fact]
    public async Task AdjacentRentalIsAllowedAndChargesExclusiveEndDate()
    {
        var db = new MemoryDb();
        var c = new Customer { UserId = "buyer" };
        db.Customers.Add(c);
        var v = new Vehicle { Name = "Camión", PricePerDay = 100 };
        db.Vehicles.Add(v);
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        db.Rentals.Add(new Rental { VehicleId = v.Id, Vehicle = v, Customer = c, StartDate = date, EndDate = date.AddDays(2) });
        var service = new SalesService(db, Mapper(), new Receipt(), new Mail());
        var r = await service.Rent(new(v.Id, date.AddDays(2), date.AddDays(4)), "buyer", false, default);
        Assert.Equal(200m, r.Total);
    }
    sealed class Receipt : IReceiptService
    {
        public Task<FileDto> GenerateAsync(Sale sale, CancellationToken ct) => Task.FromResult(new FileDto([1, 2], "application/pdf", "receipt.pdf"));
    }
    sealed class Mail : IEmailService
    {
        public Task<bool> SendReceiptAsync(string email, string number, FileDto receipt, CancellationToken ct) => Task.FromResult(false);
    }
}
public sealed class MemoryRepository<T> : IRepository<T> where T : Entity
{
    public List<T> Items { get; } = []; public void Add(T entity) => Items.Add(entity); public void Remove(T entity) => Items.Remove(entity); public Task<List<T>> ListAsync(CancellationToken ct = default) => Task.FromResult(Items); public Task<T?> FindAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(e => e.Id == id));
}
public sealed class MemoryDb : IUnitOfWork
{
    public IRepository<Product> Products { get; } = new MemoryRepository<Product>(); public IRepository<Customer> Customers { get; } = new MemoryRepository<Customer>(); public IRepository<Vehicle> Vehicles { get; } = new MemoryRepository<Vehicle>(); public List<Sale> Sales { get; } = []; public List<Rental> Rentals { get; } = [];
    public Task<List<Sale>> SalesAsync(CancellationToken ct = default) => Task.FromResult(Sales); public Task<List<Rental>> RentalsAsync(CancellationToken ct = default) => Task.FromResult(Rentals); public void AddSale(Sale s) => Sales.Add(s); public void AddRental(Rental r) => Rentals.Add(r); public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask; public Task<ITransaction> BeginAsync(CancellationToken ct = default) => Task.FromResult<ITransaction>(new Tx()); sealed class Tx : ITransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask; public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
