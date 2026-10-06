namespace Firmeza.Application.Common;

public sealed class RequestException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public static RequestException NotFound() => new("not_found", "The resource was not found.", 404);
    public static RequestException Conflict(string message) => new("conflict", message, 409);
}
