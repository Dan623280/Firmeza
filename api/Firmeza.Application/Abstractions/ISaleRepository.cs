using Firmeza.Domain.Entities;
using Firmeza.Application.Common;
namespace Firmeza.Application.Abstractions;

public interface ISaleRepository
{
    Task<Sale?> GetAsync(Guid id, bool forUpdate, CancellationToken ct);
    Task<PagedResult<Sale>> ListAsync(Guid? customerId, PaginationRequest page, CancellationToken ct);
    void Add(Sale sale);
}
