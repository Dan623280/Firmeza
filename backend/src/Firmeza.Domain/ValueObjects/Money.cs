namespace Firmeza.Domain.ValueObjects;

public readonly record struct Money(decimal Amount)
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal Tax(decimal subtotal) => Round(subtotal * 0.19m);
}
