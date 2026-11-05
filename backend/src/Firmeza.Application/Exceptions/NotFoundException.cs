namespace Firmeza.Application.Exceptions;

public sealed class NotFoundException(string resource) : Exception($"No se encontró {resource}.");
