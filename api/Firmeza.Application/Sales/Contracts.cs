using System.ComponentModel.DataAnnotations;
using Firmeza.Application.Abstractions;
namespace Firmeza.Application.Sales;

public sealed record SaleLineRequest(Guid ProductId, [Range(1, int.MaxValue)] int Quantity);
public sealed record CreateSaleRequest([Required, MinLength(1), MaxLength(100)] List<SaleLineRequest> Items);
public sealed record SaleItemResponse(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TaxRate, decimal Subtotal, decimal TaxTotal);
public sealed record SaleResponse(Guid Id, Guid CustomerId, string Number, DateTimeOffset CreatedAt, string Status, decimal Subtotal, decimal TaxTotal, decimal Total, IReadOnlyList<SaleItemResponse> Items);
public sealed record SaleDeliveryResponse(SaleResponse Sale, DeliveryStatus? Delivery);
