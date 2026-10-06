using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Constants;
using Firmeza.Domain.Errors;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class Product : AggregateRoot
{
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal Price
    {
        get; private set;
    }
    public decimal TaxRate
    {
        get; private set;
    }
    public int Stock
    {
        get; private set;
    }
    public bool IsActive { get; private set; } = true;
    private Product()
    {
    }
    public static Product Create(string name, string description, Money price, int stock, decimal taxRate)
    {
        Guard.Require(stock >= 0, "Stock cannot be negative.");
        var product = new Product { Stock = stock };
        product.Update(name, description, price, taxRate);
        return product;
    }
    public void Update(string name, string description, Money price, decimal taxRate)
    {
        Guard.Require(price.Amount > 0, "Product price must be positive.");
        Guard.Require(taxRate >= 0 && taxRate <= 1 && decimal.Round(taxRate, 4) == taxRate, "Tax rate must be between zero and one with at most four decimal places.");
        var validName = Guard.Text(name, "Name", BusinessConstants.ProductNameLength);
        var validDescription = Guard.Text(description, "Description", BusinessConstants.DescriptionLength, false);
        Name = validName;
        Description = validDescription;
        Price = price.Amount;
        TaxRate = taxRate;
        Touch();
    }
    public void SetStock(int stock)
    {
        Guard.Require(stock >= 0, "Stock cannot be negative.");
        Stock = stock;
        Touch();
    }
    public void ReduceStock(int quantity)
    {
        Guard.Require(quantity > 0, "Quantity must be positive.");
        if (!IsActive)
            throw new DomainException(DomainErrors.Inactive);
        if (quantity > Stock)
            throw new DomainException(DomainErrors.InsufficientStock);
        Stock -= quantity;
        Touch();
    }
    public void RestoreStock(int quantity)
    {
        Guard.Require(quantity > 0 && Stock <= int.MaxValue - quantity, "Stock restoration exceeds valid integer stock.");
        Stock += quantity;
        Touch();
    }
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
