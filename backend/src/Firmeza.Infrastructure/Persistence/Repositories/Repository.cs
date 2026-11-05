using Firmeza.Application.Interfaces;
using Firmeza.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
namespace Firmeza.Infrastructure.Persistence.Repositories;

public sealed class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    public Task<List<T>> ListAsync(CancellationToken ct = default) => db.Set<T>().ToListAsync(ct);
    public async Task<T?> FindAsync(Guid id, CancellationToken ct = default) => await db.Set<T>().FindAsync([id], ct);
    public void Add(T entity) => db.Set<T>().Add(entity);
    public void Remove(T entity) => db.Set<T>().Remove(entity);
}
public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public IRepository<Product> Products { get; } = new Repository<Product>(db);
    public IRepository<Customer> Customers { get; } = new Repository<Customer>(db);
    public IRepository<Vehicle> Vehicles { get; } = new Repository<Vehicle>(db);
    public Task<List<Sale>> SalesAsync(CancellationToken ct = default) => db.Sales.Include(s => s.Customer).Include(s => s.Details).ToListAsync(ct);
    public Task<List<Rental>> RentalsAsync(CancellationToken ct = default) => db.Rentals.Include(r => r.Customer).Include(r => r.Vehicle).ToListAsync(ct);
    public void AddSale(Sale sale) => db.Sales.Add(sale); public void AddRental(Rental rental) => db.Rentals.Add(rental);
    public async Task SaveAsync(CancellationToken ct = default) => await db.SaveChangesAsync(ct);
    public async Task<ITransaction> BeginAsync(CancellationToken ct = default) => new Transaction(await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct));
    private sealed class Transaction(IDbContextTransaction tx) : ITransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => tx.CommitAsync(ct); public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
