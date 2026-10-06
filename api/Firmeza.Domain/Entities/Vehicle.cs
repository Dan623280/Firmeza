using Firmeza.Domain.Abstractions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class Vehicle : AggregateRoot
{
    public string Name { get; private set; } = "";
    public string Type { get; private set; } = "";
    public string Brand { get; private set; } = "";
    public string LicensePlate { get; private set; } = "";
    public decimal DailyPrice
    {
        get; private set;
    }
    public bool IsActive { get; private set; } = true;
    private Vehicle()
    {
    }
    public static Vehicle Create(string name, string type, string brand, string plate, Money dailyPrice)
    {
        var vehicle = new Vehicle();
        vehicle.Update(name, type, brand, plate, dailyPrice);
        return vehicle;
    }
    public void Update(string name, string type, string brand, string plate, Money dailyPrice)
    {
        Guard.Require(dailyPrice.Amount > 0, "Daily price must be positive.");
        var validName = Guard.Text(name, "Name", 150);
        var validType = Guard.Text(type, "Type", 100);
        var validBrand = Guard.Text(brand, "Brand", 100);
        var validPlate = Guard.Text(plate, "License plate", 20).ToUpperInvariant();
        Guard.Require(validPlate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'), "License plate is invalid.");
        Name = validName;
        Type = validType;
        Brand = validBrand;
        LicensePlate = validPlate;
        DailyPrice = dailyPrice.Amount;
        Touch();
    }
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
