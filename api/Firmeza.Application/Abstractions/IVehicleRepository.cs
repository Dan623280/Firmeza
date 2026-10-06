using Firmeza.Domain.Entities;
using Firmeza.Application.Common;
namespace Firmeza.Application.Abstractions;

public interface IVehicleRepository
{
    Task<Vehicle?> GetAsync(Guid id, bool forUpdate, CancellationToken ct);
    Task<PagedResult<Vehicle>> ListAsync(PaginationRequest page, CancellationToken ct);
    void Add(Vehicle entity);

}
