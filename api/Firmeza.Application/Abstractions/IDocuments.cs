using Firmeza.Application.Sales;
using Firmeza.Application.Customers;
namespace Firmeza.Application.Abstractions;

public interface IReceiptGenerator
{
    Task<byte[]> GenerateAsync(SaleResponse sale, CustomerResponse customer, CancellationToken ct);
}
public interface IReceiptStorage
{
    Task<string> SaveAsync(Guid saleId, byte[] contents, CancellationToken ct); Task<Stream> OpenReadAsync(string key, CancellationToken ct);
}
public interface IReceiptReader
{
    Task<Stream> OpenAsync(Guid saleId, CancellationToken ct);
}
public interface IEmailSender
{
    Task SendReceiptAsync(string recipient, string saleNumber, byte[] pdf, CancellationToken ct);
}
