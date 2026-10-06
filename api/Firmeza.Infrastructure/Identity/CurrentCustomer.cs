using System.Security.Claims;
using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Microsoft.AspNetCore.Http;
namespace Firmeza.Infrastructure.Identity;

public sealed class CurrentCustomer(IHttpContextAccessor accessor, ICustomerRepository customers) : ICurrentCustomer
{
    public async Task<Guid> GetRequiredCustomerIdAsync(CancellationToken ct)
    {
        var id = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null)
            throw new RequestException("unauthorized", "Authentication is required.", 401);
        var customer = await customers.FindByUserAsync(id, ct);
        if (customer is null || !customer.IsActive)
            throw new RequestException("forbidden", "An active customer profile is required.", 403);
        return customer.Id;
    }
}
