namespace Firmeza.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
public interface IBusinessTransaction
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
public interface ICurrentCustomer
{
    Task<Guid> GetRequiredCustomerIdAsync(CancellationToken ct);
}
