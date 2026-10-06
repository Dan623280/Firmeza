using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Firmeza.Domain.Exceptions;
using Xunit;
namespace Firmeza.Tests.Domain;

public class BusinessRulesTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(10000000000)]
    public void Money_rejects_out_of_range_amounts(decimal amount) => Assert.Throws<DomainException>(() => Money.Create(amount));
    [Fact] public void Money_rejects_fractional_cents() => Assert.Throws<DomainException>(() => Money.Create(1.001m));
    [Fact]
    public void Product_prevents_overselling()
    {
        var product = Product.Create("Hammer", "Steel hammer", Money.Create(100), 1, 0.19m);
        product.ReduceStock(1);
        Assert.Equal(0, product.Stock);
        Assert.Throws<DomainException>(() => product.ReduceStock(1));
    }
    [Fact]
    public void Product_rejects_stock_overflow()
    {
        var product = Product.Create("Hammer", "", Money.Create(100), int.MaxValue, 0);
        Assert.Throws<DomainException>(() => product.RestoreStock(1));
    }
    [Fact]
    public void Sale_snapshots_price_and_calculates_tax()
    {
        var product = Product.Create("Hammer", "", Money.Create(100), 5, 0.19m);
        var sale = Sale.Create(Guid.NewGuid(), "F-001", [SaleItem.Create(product, 3)]);
        product.Update("Hammer", "", Money.Create(200), 0);
        Assert.Equal(300m, sale.Subtotal);
        Assert.Equal(57m, sale.TaxTotal);
        Assert.Equal(357m, sale.Total);
        Assert.Equal(100m, sale.Items.Single().UnitPrice);
    }
    [Fact]
    public void Sale_cancellation_is_a_single_transition()
    {
        var sale = Sale.Create(Guid.NewGuid(), "F-001", [SaleItem.Create(Product.Create("Hammer", "", Money.Create(100), 2, 0), 1)]);
        sale.Cancel();
        Assert.Throws<DomainException>(() => sale.Cancel());
    }
    [Fact]
    public void Sale_rejects_duplicate_items()
    {
        var p = Product.Create("Hammer", "", Money.Create(100), 5, 0);
        Assert.Throws<DomainException>(() => Sale.Create(Guid.NewGuid(), "F-001", [SaleItem.Create(p, 1), SaleItem.Create(p, 1)]));
    }
    [Fact]
    public void Rental_period_is_end_exclusive()
    {
        var period = RentalPeriod.Create(new(2026, 6, 1), new(2026, 6, 3));
        Assert.Equal(2, period.Days);
        Assert.False(period.Overlaps(RentalPeriod.Create(new(2026, 6, 3), new(2026, 6, 5))));
        Assert.True(period.Overlaps(RentalPeriod.Create(new(2026, 6, 2), new(2026, 6, 4))));
        Assert.Throws<DomainException>(() => RentalPeriod.Create(new(2026, 6, 1), new(2026, 6, 1)));
        var rental = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), period, Money.Create(100));
        Assert.Equal(200m, rental.Total);
        rental.Complete();
        Assert.Throws<DomainException>(() => rental.Cancel());
    }
    [Fact]
    public void Customer_validates_email_and_document()
    {
        Assert.Throws<DomainException>(() => Customer.Create("", "Jane", "Doe", "bad", "1234567", "Street"));
        Assert.Throws<DomainException>(() => Customer.Create("123", "Jane", "Doe", "bad", "1234567", "Street"));
    }
}
