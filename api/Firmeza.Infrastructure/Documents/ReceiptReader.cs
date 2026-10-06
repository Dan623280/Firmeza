using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Documents;

public sealed class ReceiptReader(FirmezaDbContext db, IReceiptStorage storage) : IReceiptReader
{
    public async Task<Stream> OpenAsync(Guid saleId, CancellationToken ct)
    {
        var key = await db.OutboxMessages.Where(x => x.SaleId == saleId).Select(x => x.ReceiptKey).SingleOrDefaultAsync(ct);
        if (key is null)
            throw RequestException.Conflict("Receipt is not ready yet; check delivery status.");
        return await storage.OpenReadAsync(key, ct);
    }
}
