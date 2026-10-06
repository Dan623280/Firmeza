using Firmeza.Application.Abstractions;
using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
namespace Firmeza.Infrastructure.Identity;

public sealed class AuthService(UserManager<ApplicationUser> users, ICustomerRepository customers, IBusinessTransaction transaction, IUnitOfWork work, JwtTokenService tokens) : IAuthService
{
    public Task<AuthResponse> RegisterAsync(RegisterRequest r, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var customer = Customer.Create(r.DocumentNumber, r.FirstName, r.LastName, r.Email, r.Phone, r.Address);
        if (await customers.FindByDocumentAsync(customer.DocumentNumber, token) is not null)
            throw RequestException.Conflict("Document is already registered; account linking requires administrator verification.");
        var user = new ApplicationUser { UserName = customer.Email, Email = customer.Email };
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded)
            throw new RequestException("registration_failed", string.Join(" ", result.Errors.Select(e => e.Description)));
        result = await users.AddToRoleAsync(user, Roles.Customer);
        if (!result.Succeeded)
            throw new RequestException("registration_failed", "Customer role could not be assigned.");
        customer.LinkUser(user.Id);
        customers.Add(customer);
        await work.SaveChangesAsync(token);
        return tokens.Create(user, [Roles.Customer], customer.Id);
    }, ct);
    public async Task<AuthResponse> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(r.Email.Trim());
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
            throw InvalidCredentials();
        if (!await users.CheckPasswordAsync(user, r.Password))
        {
            await users.AccessFailedAsync(user);
            throw InvalidCredentials();
        }
        var customer = await customers.FindByUserAsync(user.Id, ct);
        var roles = (await users.GetRolesAsync(user)).ToArray();
        if (roles.Contains(Roles.Customer) && (customer is null || !customer.IsActive))
            throw InvalidCredentials();
        await users.ResetAccessFailedCountAsync(user);
        return tokens.Create(user, roles, customer?.Id);
    }
    private static RequestException InvalidCredentials() => new("invalid_credentials", "Email or password is incorrect.", 401);
}
