using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Errors;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class Rental : AggregateRoot
{
    public Guid CustomerId
    {
        get; private set;
    }
    public Guid VehicleId
    {
        get; private set;
    }
    public DateOnly StartDate
    {
        get; private set;
    }
    public DateOnly EndDate
    {
        get; private set;
    }
    public decimal DailyPrice
    {
        get; private set;
    }
    public decimal Total
    {
        get; private set;
    }
    public RentalStatus Status { get; private set; } = RentalStatus.Reserved;
    private Rental()
    {
    }
    public static Rental Create(Guid customerId, Guid vehicleId, RentalPeriod period, Money dailyPrice)
    {
        Guard.Require(customerId != Guid.Empty && vehicleId != Guid.Empty, "Customer and vehicle are required.");
        Guard.Require(dailyPrice.Amount > 0, "Daily price must be positive.");
        return new()
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            StartDate = period.Start,
            EndDate = period.End,
            DailyPrice = dailyPrice.Amount,
            Total = Money.Create(period.Days * dailyPrice.Amount).Amount
        };
    }
    public void Cancel()
    {
        Transition(RentalStatus.Cancelled);
    }
    public void Complete()
    {
        Transition(RentalStatus.Completed);
    }
    private void Transition(RentalStatus status)
    {
        if (Status != RentalStatus.Reserved)
            throw new DomainException(DomainErrors.InvalidTransition);
        Status = status;
        Touch();
    }
}
