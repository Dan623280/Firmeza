using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/products"), Authorize(Roles = Roles.Administrator)]
public sealed class ProductsController(ProductService service) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ProductResponse>>> List([FromQuery] PaginationRequest page, CancellationToken ct)
    {
        page.IncludeInactive = page.IncludeInactive && User.IsInRole(Roles.Administrator);
        return Ok(await service.ListAsync(page, ct));
    }
    [HttpGet("{id:guid}")][AllowAnonymous] public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, User.IsInRole(Roles.Administrator), ct));
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = item.Id
        }, item);
    }
    [HttpPut("{id:guid}")] public async Task<ActionResult<ProductResponse>> Update(Guid id, ProductRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(id, request, ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }
}
