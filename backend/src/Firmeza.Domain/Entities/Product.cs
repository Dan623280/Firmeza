using Firmeza.Domain.Abstractions;
using Firmeza.Domain.Exceptions;
namespace Firmeza.Domain.Entities;

public sealed class Product : Entity
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price
    {
        get; set;
    }
    public int Stock
    {
        get; set;
    }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new BusinessException("La cantidad debe ser positiva.");
        if (!Active || Stock < quantity)
            throw new BusinessException($"Stock insuficiente: {Name}.", 409);
        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
