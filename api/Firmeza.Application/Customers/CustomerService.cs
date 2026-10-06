using AutoMapper;
using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Application.Customers;

public sealed class CustomerService(ICustomerRepository repository, IUnitOfWork work, IBusinessTransaction transaction, IMapper mapper)
{
    public Task<CustomerResponse> CreateAsync(CustomerRequest r, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var entity = Customer.Create(r.DocumentNumber, r.FirstName, r.LastName, r.Email, r.Phone, r.Address);
        repository.Add(entity);
        await work.SaveChangesAsync(token);
        return mapper.Map<CustomerResponse>(entity);
    }, ct);
    public async Task<CustomerResponse> GetAsync(Guid id, bool includeInactive, CancellationToken ct)
    {
        var entity = await repository.GetAsync(id, false, ct) ?? throw RequestException.NotFound();
        if (!includeInactive && !entity.IsActive)
            throw RequestException.NotFound();
        return mapper.Map<CustomerResponse>(entity);
    }
    public async Task<PagedResult<CustomerResponse>> ListAsync(PaginationRequest page, CancellationToken ct)
    {
        page.Validate();
        var result = await repository.ListAsync(page, ct);
        return new(mapper.Map<List<CustomerResponse>>(result.Items), result.Total, result.Page, result.PageSize);
    }
    public Task<CustomerResponse> UpdateAsync(Guid id, CustomerRequest r, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var entity = await repository.GetAsync(id, true, token) ?? throw RequestException.NotFound();
        entity.Update(r.DocumentNumber, r.FirstName, r.LastName, r.Email, r.Phone, r.Address);
        await work.SaveChangesAsync(token);
        return mapper.Map<CustomerResponse>(entity);
    }, ct);
    public Task<bool> DeactivateAsync(Guid id, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var entity = await repository.GetAsync(id, true, token) ?? throw RequestException.NotFound();
        entity.Deactivate();
        await work.SaveChangesAsync(token);
        return true;
    }, ct);
}
