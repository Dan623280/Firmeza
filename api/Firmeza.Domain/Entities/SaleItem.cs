using Firmeza.Domain.Abstractions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class SaleItem : Entity
{
    public Guid SaleId
    {
        get; private set;
    }
    public Guid ProductId
    {
        get; private set;
    }
    public string ProductName { get; private set; } = "";
    public int Quantity
    {
        get; private set;
    }
    public decimal UnitPrice
    {
        get; private set;
    }
    public decimal TaxRate
    {
        get; private set;
    }
    public decimal Subtotal
    {
        get; private set;
    }
    public decimal TaxTotal
    {
        get; private set;
    }
    private SaleItem()
    {
    }
    public static SaleItem Create(Product product, int quantity)
    {
        Guard.Require(quantity > 0, "Quantity must be positive.");
        Guard.Require(product.IsActive, "Product must be active.");
        var subtotal = Money.Create(product.Price * quantity).Amount;
        var tax = Money.Create(decimal.Round(subtotal * product.TaxRate, 2, MidpointRounding.AwayFromZero)).Amount;
        return new()
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            UnitPrice = product.Price,
            TaxRate = product.TaxRate,
            Subtotal = subtotal,
            TaxTotal = tax
        };
    }
    internal void AssignSale(Guid saleId)
    {
        Guard.Require(SaleId == Guid.Empty, "Sale item already belongs to a sale.");
        SaleId = saleId;
    }
}
