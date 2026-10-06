using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Errors;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class Sale : AggregateRoot
{
    private readonly List<SaleItem> _items = [];
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();
    public Guid CustomerId
    {
        get; private set;
    }
    public string Number { get; private set; } = "";
    public decimal Subtotal
    {
        get; private set;
    }
    public decimal TaxTotal
    {
        get; private set;
    }
    public decimal Total
    {
        get; private set;
    }
    public SaleStatus Status { get; private set; } = SaleStatus.Completed;
    private Sale()
    {
    }
    public static Sale Create(Guid customerId, string number, IEnumerable<SaleItem> items)
    {
        Guard.Require(customerId != Guid.Empty, "Customer ID is required.");
        var lines = items.ToList();
        Guard.Require(lines.Count is > 0 and <= 100, "A sale requires 1–100 items.");
        Guard.Require(lines.Select(i => i.ProductId).Distinct().Count() == lines.Count, "Duplicate product lines are not allowed.");
        var sale = new Sale { CustomerId = customerId, Number = Guard.Text(number, "Sale number", 50) };
        sale.Subtotal = Money.Create(lines.Sum(i => i.Subtotal)).Amount;
        sale.TaxTotal = Money.Create(lines.Sum(i => i.TaxTotal)).Amount;
        sale.Total = Money.Create(sale.Subtotal + sale.TaxTotal).Amount;
        foreach (var line in lines)
        {
            line.AssignSale(sale.Id);
            sale._items.Add(line);
        }
        return sale;
    }
    public void Cancel()
    {
        if (Status != SaleStatus.Completed)
            throw new DomainException(DomainErrors.InvalidTransition);
        Status = SaleStatus.Cancelled;
        Touch();
    }
}
