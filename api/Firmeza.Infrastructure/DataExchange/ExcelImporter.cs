using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Application.DataExchange;
using Firmeza.Application.Sales;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Firmeza.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Firmeza.Infrastructure.Persistence;
namespace Firmeza.Infrastructure.DataExchange;

internal sealed class ExcelImporter(ExcelReader reader, IProductRepository products, ICustomerRepository customers, SaleService sales, IUnitOfWork work, IBusinessTransaction transaction, IIdempotencyStore idempotency, FirmezaDbContext db)
{
    public async Task<ImportReport> ImportAsync(Stream stream, CancellationToken ct)
    {
        var (records, errors, hash) = await reader.ReadAsync(stream, ct);
        if (errors.Count > 0)
            return new(0, errors);
        var currentRow = 0;
        try
        {
            return await transaction.ExecuteAsync(async token =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"import:" + hash},0))", token);
                var previousBatch = await db.ImportBatches.AsNoTracking().SingleOrDefaultAsync(x => x.Hash == hash, token);
                if (previousBatch is not null)
                    return new ImportReport(previousBatch.AppliedRows, []);
                // A changed workbook must not change stock around an already posted sale.
                foreach (var group in records.Where(x => x.Type == "sale").GroupBy(x => x.Reference))
                {
                    var saleRow = group.First();
                    currentRow = saleRow.Row;
                    Guid? customerId = null;
                    var customerRow = records.SingleOrDefault(x => x.Type == "customer" && x.Reference == saleRow.CustomerReference);
                    if (customerRow is not null)
                    {
                        var customer = customerRow.Id.HasValue ? await customers.GetAsync(customerRow.Id.Value, false, token) : await customers.FindByDocumentAsync(DocumentNumber.Create(customerRow.Customer!.DocumentNumber).Value, token);
                        customerId = customer?.Id;
                    }
                    else if (Guid.TryParse(saleRow.CustomerReference, out var directId))
                        customerId = directId;
                    if (customerId.HasValue)
                    {
                        await idempotency.LockAsync(customerId.Value, saleRow.IdempotencyKey, token);
                        if (await idempotency.FindAsync(customerId.Value, saleRow.IdempotencyKey, token) is not null)
                            throw RequestException.Conflict("A changed workbook cannot reuse an existing sale idempotency key. Re-upload the original workbook or supply a new sale key.");
                    }
                }
                var productIds = new Dictionary<string, Guid>();
                var customerIds = new Dictionary<string, Guid>();
                foreach (var row in records.Where(x => x.Type != "sale"))
                {
                    currentRow = row.Row;
                    if (row.Product is { } p)
                    {
                        Product entity;
                        if (row.Id.HasValue)
                        {
                            entity = await products.GetAsync(row.Id.Value, true, token) ?? throw RequestException.NotFound();
                            entity.Update(p.Name, p.Description, Money.Create(p.Price), p.TaxRate);
                            entity.SetStock(p.Stock);
                        }
                        else
                        {
                            entity = Product.Create(p.Name, p.Description, Money.Create(p.Price), p.Stock, p.TaxRate);
                            products.Add(entity);
                        }
                        productIds.Add(row.Reference, entity.Id);
                    }
                    else if (row.Customer is { } c)
                    {
                        var document = DocumentNumber.Create(c.DocumentNumber).Value;
                        var entity = row.Id.HasValue ? await customers.GetAsync(row.Id.Value, true, token) ?? throw RequestException.NotFound() : await customers.FindByDocumentAsync(document, token);
                        if (entity is null)
                        {
                            entity = Customer.Create(c.DocumentNumber, c.FirstName, c.LastName, c.Email, c.Phone, c.Address);
                            customers.Add(entity);
                        }
                        else
                        {
                            await customers.GetAsync(entity.Id, true, token);
                            entity.Update(c.DocumentNumber, c.FirstName, c.LastName, c.Email, c.Phone, c.Address);
                        }
                        customerIds.Add(row.Reference, entity.Id);
                    }
                }
                await work.SaveChangesAsync(token);
                foreach (var group in records.Where(x => x.Type == "sale").GroupBy(x => x.Reference))
                {
                    currentRow = group.First().Row;
                    var first = group.First();
                    Guid Resolve(string reference, Dictionary<string, Guid> ids) => ids.TryGetValue(reference, out var id) ? id : Guid.TryParse(reference, out id) ? id : throw new RequestException("missing_reference", $"Reference {reference} was not found.");
                    var customerId = Resolve(first.CustomerReference, customerIds);
                    var request = new CreateSaleRequest(group.Select(row => new SaleLineRequest(Resolve(row.ProductReference, productIds), row.Quantity)).ToList());
                    await sales.CreateAsync(customerId, request, first.IdempotencyKey, token);
                }
                db.ImportBatches.Add(new()
                {
                    Hash = hash,
                    AppliedRows = records.Count
                });
                await work.SaveChangesAsync(token);
                return new ImportReport(records.Count, []);
            }, ct);
        }
        catch (Exception ex) when (ex is RequestException or DomainException or DbUpdateException or OverflowException or PostgresException)
        {
            var message = ex is DbUpdateException or PostgresException ? "Import conflicts with stored records; no rows were applied." : ex.Message;
            return new(0, [new(currentRow, "import_failed", message)]);
        }
    }
}
