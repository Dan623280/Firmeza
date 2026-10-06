using Firmeza.Application.Abstractions;
using Firmeza.Infrastructure.Email;
using Firmeza.Infrastructure.Persistence;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Firmeza.Tests.Integration;

public class DeliveryTests
{
    [DatabaseFact]
    public async Task Smtp_failure_does_not_rollback_sale_and_stops_after_five_attempts()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        Guid saleId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
            var customer = Customer.Create("123456789", "Jane", "Doe", "jane@example.test", "123456789", "Street");
            var product = Product.Create("Hammer", "", Money.Create(100), 1, 0);
            var sale = Sale.Create(customer.Id, "DELIVERY-1", [SaleItem.Create(product, 1)]);
            saleId = sale.Id;
            db.AddRange(customer, product, sale, new OutboxMessage { SaleId = sale.Id });
            await db.SaveChangesAsync();
        }
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FirmezaDbContext>();
            await db.OutboxMessages.ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
            var processor = ActivatorUtilities.CreateInstance<DeliveryProcessor>(scope.ServiceProvider, new FailingEmailSender());
            Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
            var msg = await db.OutboxMessages.AsNoTracking().SingleAsync();
            Assert.Equal(attempt, msg.Attempts);
            Assert.Equal(attempt == 5 ? "Failed" : "Pending", msg.Status);
            Assert.Equal(1, await db.Sales.CountAsync());
            Assert.NotNull(msg.ReceiptKey);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var processor = ActivatorUtilities.CreateInstance<DeliveryProcessor>(scope.ServiceProvider, new FailingEmailSender());
            Assert.False(await processor.ProcessNextAsync(CancellationToken.None));
        }
    }
    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendReceiptAsync(string recipient, string saleNumber, byte[] pdf, CancellationToken ct) => throw new IOException("SMTP unavailable in test");
    }
}
