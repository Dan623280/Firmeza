using Firmeza.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class UnitOfWork(FirmezaDbContext db) : IUnitOfWork, IBusinessTransaction
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
            return await action(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); db.ChangeTracker.Clear(); throw; }
    }
}
