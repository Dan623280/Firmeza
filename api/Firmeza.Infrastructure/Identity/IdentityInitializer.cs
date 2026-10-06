using Firmeza.Application.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
namespace Firmeza.Infrastructure.Identity;

public sealed class IdentityInitializer(RoleManager<IdentityRole> roles, UserManager<ApplicationUser> users, IConfiguration config)
{
    public async Task InitializeAsync()
    {
        foreach (var role in new[] { Roles.Administrator, Roles.Customer })
            if (!await roles.RoleExistsAsync(role))
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException("Unable to initialize roles.");
            }
        var email = config["BootstrapAdmin:Email"];
        var password = config["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;
        if (await users.FindByEmailAsync(email) is not null)
            return;
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException("Administrator bootstrap failed: check configured password requirements.");
        var assigned = await users.AddToRoleAsync(user, Roles.Administrator);
        if (!assigned.Succeeded)
            throw new InvalidOperationException("Administrator role assignment failed.");
    }
}
