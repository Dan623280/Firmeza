using Firmeza.Application.DTOs;
using Firmeza.Application.Services;
using Firmeza.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.API.Controllers;

[ApiController, Route("api/productos")]
public sealed class ProductsController(CatalogService service) : ControllerBase
{
    [HttpGet, AllowAnonymous] public Task<List<ProductDto>> List(string? search, CancellationToken ct) => service.Products(search, User.IsInRole(Roles.Administrator), ct);
    [HttpGet("{id:guid}"), AllowAnonymous]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct)
    {
        var p = (await service.Products(null, User.IsInRole(Roles.Administrator), ct)).FirstOrDefault(p => p.Id == id);
        return p == null ? NotFound() : Ok(p);
    }
    [HttpPost, Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<ProductDto>> Create(ProductInput input, CancellationToken ct)
    {
        var p = await service.SaveProduct(null, input, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = p.Id
        }, p);
    }
    [HttpPut("{id:guid}"), Authorize(Roles = Roles.Administrator)] public Task<ProductDto> Update(Guid id, ProductInput input, CancellationToken ct) => service.SaveProduct(id, input, ct);
    [HttpDelete("{id:guid}"), Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteProduct(id, ct);
        return NoContent();
    }
}
