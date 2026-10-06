using Microsoft.AspNetCore.Identity;
namespace Firmeza.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
}
