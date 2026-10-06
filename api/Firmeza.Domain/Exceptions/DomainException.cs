using Firmeza.Domain.Errors;
namespace Firmeza.Domain.Exceptions;

public sealed class DomainException(BusinessError error) : Exception(error.Message)
{
    public BusinessError Error { get; } = error;
}
