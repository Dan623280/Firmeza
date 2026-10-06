using AutoMapper;
using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Application.Products;

public sealed class ProductService(IProductRepository repository, IUnitOfWork work, IBusinessTransaction transaction, IMapper mapper)
{
    public Task<ProductResponse> CreateAsync(ProductRequest r, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var entity = Product.Create(r.Name, r.Description, Money.Create(r.Price), r.Stock, r.TaxRate);
        repository.Add(entity);
        await work.SaveChangesAsync(token);
        return mapper.Map<ProductResponse>(entity);
    }, ct);
    public async Task<ProductResponse> GetAsync(Guid id, bool includeInactive, CancellationToken ct)
    {
        var entity = await repository.GetAsync(id, false, ct) ?? throw RequestException.NotFound();
        if (!includeInactive && !entity.IsActive)
            throw RequestException.NotFound();
        return mapper.Map<ProductResponse>(entity);
    }
    public async Task<PagedResult<ProductResponse>> ListAsync(PaginationRequest page, CancellationToken ct)
    {
        page.Validate();
        var result = await repository.ListAsync(page, ct);
        return new(mapper.Map<List<ProductResponse>>(result.Items), result.Total, result.Page, result.PageSize);
    }
    public Task<ProductResponse> UpdateAsync(Guid id, ProductRequest r, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var entity = await repository.GetAsync(id, true, token) ?? throw RequestException.NotFound();
        entity.Update(r.Name, r.Description, Money.Create(r.Price), r.TaxRate);
        entity.SetStock(r.Stock);
        await work.SaveChangesAsync(token);
        return mapper.Map<ProductResponse>(entity);
    }, ct);
    public Task<bool> DeactivateAsync(Guid id, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var entity = await repository.GetAsync(id, true, token) ?? throw RequestException.NotFound();
        entity.Deactivate();
        await work.SaveChangesAsync(token);
        return true;
    }, ct);
}
