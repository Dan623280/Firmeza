using Firmeza.Application.DTOs;
using Firmeza.Domain.Entities;
namespace Firmeza.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<List<T>> ListAsync(CancellationToken ct = default);
    Task<T?> FindAsync(Guid id, CancellationToken ct = default);
    void Add(T entity);
    void Remove(T entity);
}
public interface IUnitOfWork
{
    IRepository<Product> Products
    {
        get;
    }
    IRepository<Customer> Customers
    {
        get;
    }
    IRepository<Vehicle> Vehicles
    {
        get;
    }
    Task<List<Sale>> SalesAsync(CancellationToken ct = default);
    Task<List<Rental>> RentalsAsync(CancellationToken ct = default);
    void AddSale(Sale sale); void AddRental(Rental rental);
    Task SaveAsync(CancellationToken ct = default);
    Task<ITransaction> BeginAsync(CancellationToken ct = default);
}
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
public interface IAuthService
{
    Task<AuthDto> LoginAsync(LoginInput input, CancellationToken ct); Task<AuthDto> RegisterAsync(RegisterInput input, CancellationToken ct);
}
public interface IReceiptService
{
    Task<FileDto> GenerateAsync(Sale sale, CancellationToken ct);
}
public interface IEmailService
{
    Task<bool> SendReceiptAsync(string email, string number, FileDto receipt, CancellationToken ct);
}
public interface IExchangeService
{
    Task<ImportResult> ImportAsync(Stream stream, CancellationToken ct); Task<FileDto> ExportAsync(string resource, string format, CancellationToken ct);
}
