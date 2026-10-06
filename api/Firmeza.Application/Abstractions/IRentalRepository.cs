using Firmeza.Domain.Entities;
using Firmeza.Application.Common;
namespace Firmeza.Application.Abstractions;

public interface IRentalRepository
{
    Task<Rental?> GetAsync(Guid id, bool forUpdate, CancellationToken ct);
    Task<bool> OverlapsAsync(Guid vehicleId, DateOnly start, DateOnly end, CancellationToken ct);
    Task<PagedResult<Rental>> ListAsync(Guid? customerId, PaginationRequest page, CancellationToken ct);
    void Add(Rental rental);
}
