using Firmeza.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class IdempotencyStore(FirmezaDbContext db) : IIdempotencyStore
{
    public async Task LockAsync(Guid customerId, string key, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({customerId.ToString() + ":" + key},0))", ct);
    public async Task<(string Hash, Guid SaleId)?> FindAsync(Guid customerId, string key, CancellationToken ct)
    {
        var record = await db.IdempotencyRecords.SingleOrDefaultAsync(x => x.CustomerId == customerId && x.Key == key, ct);
        return record is null ? null : (record.Hash, record.SaleId);
    }
    public void Add(Guid customerId, string key, string hash, Guid saleId) => db.IdempotencyRecords.Add(new() { CustomerId = customerId, Key = key, Hash = hash, SaleId = saleId });
}
