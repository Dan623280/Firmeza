using Firmeza.Domain.Abstractions;
namespace Firmeza.Domain.Entities;

public sealed class Customer : Entity
{
    public string? UserId
    {
        get; set;
    }
    public string Document { get; set; } = "";
    public string Name { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public int Age
    {
        get; set;
    }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;
}
