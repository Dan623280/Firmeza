using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class SaleRepository(FirmezaDbContext db) : ISaleRepository
{
    public async Task<Sale?> GetAsync(Guid id, bool forUpdate, CancellationToken ct)
    {
        if (forUpdate)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Sales\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        return await db.Sales.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
    }
    public async Task<PagedResult<Sale>> ListAsync(Guid? customerId, PaginationRequest page, CancellationToken ct)
    {
        var q = db.Sales.AsNoTracking().Where(x => !customerId.HasValue || x.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(page.Search))
            q = q.Where(x => x.Number.Contains(page.Search));
        var total = await q.CountAsync(ct);
        return new(await q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).Include(x => x.Items).ToListAsync(ct), total, page.Page, page.PageSize);
    }
    public void Add(Sale sale) => db.Sales.Add(sale);
}
