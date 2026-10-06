using Firmeza.Domain.Entities;
using Firmeza.Application.Common;
namespace Firmeza.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, bool forUpdate, CancellationToken ct);
    Task<PagedResult<Product>> ListAsync(PaginationRequest page, CancellationToken ct);
    void Add(Product entity);

}
