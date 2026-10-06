using Firmeza.Domain.Errors;
using Firmeza.Domain.Exceptions;
namespace Firmeza.Domain.ValueObjects;

public sealed record DocumentNumber
{
    public string Value
    {
        get;
    }
    private DocumentNumber(string value) => Value = value;
    public static DocumentNumber Create(string value)
    {
        value = value?.Trim() ?? "";
        if (value.Length is < 3 or > 30 || !value.All(char.IsAsciiLetterOrDigit))
            throw new DomainException(DomainErrors.Invalid("Document number must contain 3–30 letters or digits."));
        return new(value.ToUpperInvariant());
    }
}
