using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Enums;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class Sale : Entity
{
    public Guid CustomerId
    {
        get; set;
    }
    public Customer Customer { get; set; } = null!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Number { get; set; } = "FV-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
    public decimal Subtotal
    {
        get; set;
    }
    public decimal Tax
    {
        get; set;
    }
    public decimal Total
    {
        get; set;
    }
    public SaleStatus Status { get; set; } = SaleStatus.Confirmada;
    public string? ReceiptPath
    {
        get; set;
    }
    public bool EmailSent
    {
        get; set;
    }
    public List<SaleDetail> Details { get; set; } = [];
    public void CalculateTotals()
    {
        Subtotal = Money.Round(Details.Sum(d => d.Subtotal));
        Tax = Money.Tax(Subtotal);
        Total = Subtotal + Tax;
    }
}
public sealed class SaleDetail : Entity
{
    public Guid SaleId
    {
        get; set;
    }
    public Guid ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = "";
    public int Quantity
    {
        get; set;
    }
    public decimal UnitPrice
    {
        get; set;
    }
    public decimal Subtotal
    {
        get; set;
    }
}
