using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class ProductRepository(FirmezaDbContext db) : IProductRepository
{
    public async Task<Product?> GetAsync(Guid id, bool forUpdate, CancellationToken ct)
    {
        if (forUpdate)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Products\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        var entity = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (forUpdate && entity is not null && db.Entry(entity).State == EntityState.Unchanged)
            await db.Entry(entity).ReloadAsync(ct);
        return entity;
    }
    public async Task<PagedResult<Product>> ListAsync(PaginationRequest page, CancellationToken ct)
    {
        var q = db.Products.AsNoTracking().Where(x => page.IncludeInactive || x.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search))
            q = q.Where(x => x.Name.Contains(page.Search) || x.Description.Contains(page.Search));
        var total = await q.CountAsync(ct);
        return new(await q.OrderBy(x => x.Id).Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct), total, page.Page, page.PageSize);
    }
    public void Add(Product entity) => db.Products.Add(entity);

}
