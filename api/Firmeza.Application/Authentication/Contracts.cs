using System.ComponentModel.DataAnnotations;
namespace Firmeza.Application.Authentication;

public static class Roles
{
    public const string Administrator = "Administrator"; public const string Customer = "Customer";
}
public sealed record RegisterRequest([Required, StringLength(30)] string DocumentNumber, [Required, StringLength(100)] string FirstName, [Required, StringLength(100)] string LastName, [Required, EmailAddress, StringLength(254)] string Email, [Required, StringLength(25)] string Phone, [Required, StringLength(300)] string Address, [Required, StringLength(128, MinimumLength = 12)] string Password);
public sealed record LoginRequest([Required, EmailAddress, StringLength(254)] string Email, [Required, StringLength(128)] string Password);
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles, Guid? CustomerId);
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}
