using Firmeza.Domain.Errors;
using Firmeza.Domain.Exceptions;
namespace Firmeza.Domain.Abstractions;

public static class Guard
{
    public static string Text(string? value, string field, int maximum, bool required = true)
    {
        value = value?.Trim() ?? "";
        if ((required && value.Length == 0) || value.Length > maximum)
            throw new DomainException(DomainErrors.Invalid($"{field} must contain {(required ? 1 : 0)}–{maximum} characters."));
        return value;
    }
    public static void Require(bool condition, string message)
    {
        if (!condition)
            throw new DomainException(DomainErrors.Invalid(message));
    }
}
