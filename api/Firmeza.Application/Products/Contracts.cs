using System.ComponentModel.DataAnnotations;
namespace Firmeza.Application.Products;

public sealed record ProductRequest([Required, StringLength(150)] string Name, [StringLength(500)] string Description, [Range(typeof(decimal), "0.01", "9999999999.99")] decimal Price, [Range(0, int.MaxValue)] int Stock, [Range(typeof(decimal), "0", "1")] decimal TaxRate);
public sealed record ProductResponse(Guid Id, string Name, string Description, decimal Price, int Stock, decimal TaxRate, bool IsActive);
