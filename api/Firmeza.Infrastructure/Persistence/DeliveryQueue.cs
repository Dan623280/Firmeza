using Firmeza.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class DeliveryQueue(FirmezaDbContext db) : IDeliveryQueue
{
    public void Enqueue(Guid saleId) => db.OutboxMessages.Add(new() { SaleId = saleId });
    public async Task<DeliveryStatus?> GetStatusAsync(Guid saleId, CancellationToken ct)
    {
        var msg = await db.OutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.SaleId == saleId, ct);
        return msg is null ? null : new(msg.Status, msg.Attempts);
    }
}
