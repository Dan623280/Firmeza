using Firmeza.Domain.Errors;
using Firmeza.Domain.Exceptions;
namespace Firmeza.Domain.ValueObjects;

public sealed record RentalPeriod
{
    public DateOnly Start
    {
        get;
    }
    public DateOnly End
    {
        get;
    }
    public int Days => End.DayNumber - Start.DayNumber;
    private RentalPeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }
    public static RentalPeriod Create(DateOnly start, DateOnly end)
    {
        if (end <= start || end.DayNumber - start.DayNumber > 366)
            throw new DomainException(DomainErrors.Invalid("Rental must last from one to 366 days, with an exclusive end date."));
        return new(start, end);
    }
    public bool Overlaps(RentalPeriod other) => Start < other.End && other.Start < End;
}
