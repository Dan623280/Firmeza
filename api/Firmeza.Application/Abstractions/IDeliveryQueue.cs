namespace Firmeza.Application.Abstractions;

public sealed record DeliveryStatus(string Status, int Attempts);
public interface IDeliveryQueue
{
    void Enqueue(Guid saleId);
    Task<DeliveryStatus?> GetStatusAsync(Guid saleId, CancellationToken ct);
}
public interface IIdempotencyStore
{
    Task<(string Hash, Guid SaleId)?> FindAsync(Guid customerId, string key, CancellationToken ct);
    void Add(Guid customerId, string key, string hash, Guid saleId);
    Task LockAsync(Guid customerId, string key, CancellationToken ct);
}
