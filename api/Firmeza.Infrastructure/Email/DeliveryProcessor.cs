using AutoMapper;
using Firmeza.Application.Abstractions;
using Firmeza.Application.Customers;
using Firmeza.Application.Sales;
using Firmeza.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Firmeza.Infrastructure.Email;

public sealed class DeliveryProcessor(FirmezaDbContext db, IReceiptGenerator generator, IReceiptStorage storage, IEmailSender email, IMapper mapper, ILogger<DeliveryProcessor> logger)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        OutboxMessage? message;
        var now = DateTimeOffset.UtcNow;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            await db.OutboxMessages.Where(x => x.Status == "Processing" && x.LeaseUntil < now && x.Attempts >= 5).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed").SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
            var candidates = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM \"OutboxMessages\" WHERE \"Attempts\" < 5 AND ((\"Status\" = 'Pending' AND \"NextAttemptAt\" <= {now}) OR (\"Status\" = 'Processing' AND \"LeaseUntil\" < {now})) ORDER BY \"NextAttemptAt\" LIMIT 1 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
            message = candidates.SingleOrDefault();
            if (message is null)
            {
                await tx.CommitAsync(ct);
                return false;
            }
            message.Status = "Processing";
            message.Attempts++;
            message.LeaseUntil = now.AddMinutes(2);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        var lease = message.LeaseUntil;
        try
        {
            var sale = await db.Sales.AsNoTracking().Include(x => x.Items).SingleAsync(x => x.Id == message.SaleId, ct);
            var customer = await db.Customers.AsNoTracking().SingleAsync(x => x.Id == sale.CustomerId, ct);
            var pdf = await generator.GenerateAsync(mapper.Map<SaleResponse>(sale), mapper.Map<CustomerResponse>(customer), ct);
            var key = await storage.SaveAsync(sale.Id, pdf, ct);
            var owned = db.OutboxMessages.Where(x => x.Id == message.Id && x.Status == "Processing" && x.LeaseUntil == lease);
            if (await owned.ExecuteUpdateAsync(s => s.SetProperty(x => x.ReceiptKey, key), ct) != 1)
                return true;
            await email.SendReceiptAsync(customer.Email, sale.Number, pdf, ct);
            await owned.ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Sent").SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Delivery failed for sale {SaleId}, attempt {Attempt}", message.SaleId, message.Attempts);
            var status = message.Attempts >= 5 ? "Failed" : "Pending";
            var next = DateTimeOffset.UtcNow.AddSeconds(Math.Min(3600, 30 * Math.Pow(2, message.Attempts - 1)));
            await db.OutboxMessages.Where(x => x.Id == message.Id && x.Status == "Processing" && x.LeaseUntil == lease).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status).SetProperty(x => x.NextAttemptAt, next).SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
        }
        return true;
    }
}
