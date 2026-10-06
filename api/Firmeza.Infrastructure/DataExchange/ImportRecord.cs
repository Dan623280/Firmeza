using Firmeza.Application.Products;
using Firmeza.Application.Customers;
namespace Firmeza.Infrastructure.DataExchange;

internal sealed record ImportRecord(int Row, string Type, string Reference, Guid? Id, ProductRequest? Product, CustomerRequest? Customer, string CustomerReference, string ProductReference, int Quantity, string IdempotencyKey);
