using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class RentalRepository(FirmezaDbContext db) : IRentalRepository
{
    public async Task<Rental?> GetAsync(Guid id, bool forUpdate, CancellationToken ct)
    {
        if (forUpdate)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Rentals\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        var entity = await db.Rentals.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (forUpdate && entity is not null && db.Entry(entity).State == EntityState.Unchanged)
            await db.Entry(entity).ReloadAsync(ct);
        return entity;
    }
    public Task<bool> OverlapsAsync(Guid vehicleId, DateOnly start, DateOnly end, CancellationToken ct) => db.Rentals.AnyAsync(x => x.VehicleId == vehicleId && x.Status == RentalStatus.Reserved && x.StartDate < end && start < x.EndDate, ct);
    public async Task<PagedResult<Rental>> ListAsync(Guid? customerId, PaginationRequest page, CancellationToken ct)
    {
        var q = db.Rentals.AsNoTracking().Where(x => !customerId.HasValue || x.CustomerId == customerId);
        var total = await q.CountAsync(ct);
        return new(await q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct), total, page.Page, page.PageSize);
    }
    public void Add(Rental rental) => db.Rentals.Add(rental);
}
