using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Enums;
namespace Firmeza.Domain.Entities;

public sealed class Vehicle : Entity
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Plate { get; set; } = "";
    public decimal PricePerDay
    {
        get; set;
    }
    public bool Available { get; set; } = true;
    public bool Active { get; set; } = true;
}
public sealed class Rental : Entity
{
    public Guid CustomerId
    {
        get; set;
    }
    public Customer Customer { get; set; } = null!;
    public Guid VehicleId
    {
        get; set;
    }
    public Vehicle Vehicle { get; set; } = null!;
    public DateOnly StartDate
    {
        get; set;
    }
    public DateOnly EndDate
    {
        get; set;
    }
    public decimal PricePerDay
    {
        get; set;
    }
    public decimal Total
    {
        get; set;
    }
    public RentalStatus Status { get; set; } = RentalStatus.Reservado;
}
