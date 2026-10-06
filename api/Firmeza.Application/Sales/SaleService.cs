using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
namespace Firmeza.Application.Sales;

public sealed class SaleService(ISaleRepository sales, IProductRepository products, ICustomerRepository customers, IBusinessTransaction transaction, IIdempotencyStore idempotency, IDeliveryQueue delivery, IUnitOfWork work, IMapper mapper)
{
    public Task<SaleResponse> CreateAsync(Guid customerId, CreateSaleRequest request, string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
            throw new RequestException("invalid_idempotency_key", "An Idempotency-Key containing 1–100 characters is required.");
        if (request.Items is null || request.Items.Count is < 1 or > 100 || request.Items.Any(x => x is null || x.ProductId == Guid.Empty || x.Quantity <= 0) || request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count)
            throw new RequestException("validation_failed", "Supply 1–100 distinct products with positive quantities.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.Items.OrderBy(x => x.ProductId)))));
        return transaction.ExecuteAsync(async token =>
        {
            await idempotency.LockAsync(customerId, key, token);
            var customer = await customers.GetAsync(customerId, true, token) ?? throw RequestException.NotFound();
            if (!customer.IsActive)
                throw RequestException.Conflict("Customer is inactive.");
            var previous = await idempotency.FindAsync(customerId, key, token);
            if (previous is not null)
            {
                if (previous.Value.Hash != hash)
                    throw RequestException.Conflict("Idempotency key was already used with a different request.");
                return mapper.Map<SaleResponse>(await sales.GetAsync(previous.Value.SaleId, false, token));
            }
            var lines = new List<SaleItem>();
            foreach (var line in request.Items.OrderBy(x => x.ProductId))
            {
                var product = await products.GetAsync(line.ProductId, true, token) ?? throw RequestException.NotFound();
                lines.Add(SaleItem.Create(product, line.Quantity));
                product.ReduceStock(line.Quantity);
            }
            var sale = Sale.Create(customerId, $"F-{Guid.NewGuid():N}", lines);
            sales.Add(sale);
            idempotency.Add(customerId, key, hash, sale.Id);
            delivery.Enqueue(sale.Id);
            await work.SaveChangesAsync(token);
            return mapper.Map<SaleResponse>(sale);
        }, ct);
    }
    public async Task<SaleResponse> GetAsync(Guid id, Guid? owner, CancellationToken ct) => mapper.Map<SaleResponse>(await OwnedAsync(id, owner, false, ct));
    public async Task<PagedResult<SaleResponse>> ListAsync(Guid? owner, PaginationRequest page, CancellationToken ct)
    {
        page.Validate();
        var result = await sales.ListAsync(owner, page, ct);
        return new(mapper.Map<List<SaleResponse>>(result.Items), result.Total, result.Page, result.PageSize);
    }
    public Task<SaleResponse> CancelAsync(Guid id, Guid? owner, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var sale = await OwnedAsync(id, owner, true, token);
        sale.Cancel();
        foreach (var item in sale.Items.OrderBy(x => x.ProductId))
        {
            var product = await products.GetAsync(item.ProductId, true, token) ?? throw RequestException.NotFound();
            product.RestoreStock(item.Quantity);
        }
        await work.SaveChangesAsync(token);
        return mapper.Map<SaleResponse>(sale);
    }, ct);
    private async Task<Sale> OwnedAsync(Guid id, Guid? owner, bool locked, CancellationToken ct)
    {
        var sale = await sales.GetAsync(id, locked, ct) ?? throw RequestException.NotFound();
        if (owner.HasValue && sale.CustomerId != owner)
            throw RequestException.NotFound();
        return sale;
    }
}
