namespace Firmeza.Domain.Errors;

public sealed record BusinessError(string Code, string Message);
public static class DomainErrors
{
    public static BusinessError Invalid(string message) => new("validation_failed", message);
    public static readonly BusinessError InsufficientStock = new("insufficient_stock", "The requested quantity exceeds available stock.");
    public static readonly BusinessError InvalidTransition = new("invalid_transition", "The requested status transition is not allowed.");
    public static readonly BusinessError Inactive = new("inactive_resource", "The resource is inactive.");
}
