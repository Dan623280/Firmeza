using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class CustomerRepository(FirmezaDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetAsync(Guid id, bool forUpdate, CancellationToken ct)
    {
        if (forUpdate)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Customers\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        var entity = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (forUpdate && entity is not null && db.Entry(entity).State == EntityState.Unchanged)
            await db.Entry(entity).ReloadAsync(ct);
        return entity;
    }
    public async Task<PagedResult<Customer>> ListAsync(PaginationRequest page, CancellationToken ct)
    {
        var q = db.Customers.AsNoTracking().Where(x => page.IncludeInactive || x.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search))
            q = q.Where(x => x.FirstName.Contains(page.Search) || x.LastName.Contains(page.Search) || x.DocumentNumber.Contains(page.Search));
        var total = await q.CountAsync(ct);
        return new(await q.OrderBy(x => x.Id).Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct), total, page.Page, page.PageSize);
    }
    public void Add(Customer entity) => db.Customers.Add(entity);
    public Task<Customer?> FindByUserAsync(string userId, CancellationToken ct) => db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public Task<Customer?> FindByDocumentAsync(string document, CancellationToken ct) => db.Customers.SingleOrDefaultAsync(x => x.DocumentNumber == document, ct);
}
