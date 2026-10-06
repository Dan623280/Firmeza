using Firmeza.Domain.Entities;
using Firmeza.Application.Common;
namespace Firmeza.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetAsync(Guid id, bool forUpdate, CancellationToken ct);
    Task<PagedResult<Customer>> ListAsync(PaginationRequest page, CancellationToken ct);
    void Add(Customer entity);
    Task<Customer?> FindByUserAsync(string userId, CancellationToken ct); Task<Customer?> FindByDocumentAsync(string document, CancellationToken ct);
}
