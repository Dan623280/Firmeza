using Firmeza.Domain.Constants;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.Errors;
namespace Firmeza.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount
    {
        get;
    }
    public string Currency
    {
        get;
    }
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    public static Money Create(decimal amount, string currency = BusinessConstants.Currency)
    {
        if (amount < 0 || amount > BusinessConstants.MaximumMoney || decimal.Round(amount, 2) != amount)
            throw new DomainException(DomainErrors.Invalid("Money must be non-negative, fit decimal(12,2), and contain at most two decimal places."));
        if (currency != BusinessConstants.Currency)
            throw new DomainException(DomainErrors.Invalid("Only COP is supported in this version."));
        return new(amount, currency);
    }
}
