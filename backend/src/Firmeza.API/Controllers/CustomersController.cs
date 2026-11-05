using Firmeza.Application.DTOs;
using Firmeza.Application.Services;
using Firmeza.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.API.Controllers;

[ApiController, Route("api/clientes"), Authorize(Roles = Roles.Administrator)]
public sealed class CustomersController(CatalogService service) : ControllerBase
{
    [HttpGet] public Task<List<CustomerDto>> List(string? search, CancellationToken ct) => service.Customers(search, ct);
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct)
    {
        var c = (await service.Customers(null, ct)).FirstOrDefault(c => c.Id == id);
        return c == null ? NotFound() : Ok(c);
    }
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CustomerInput input, CancellationToken ct)
    {
        var c = await service.SaveCustomer(null, input, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = c.Id
        }, c);
    }
    [HttpPut("{id:guid}")] public Task<CustomerDto> Update(Guid id, CustomerInput input, CancellationToken ct) => service.SaveCustomer(id, input, ct);
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteCustomer(id, ct);
        return NoContent();
    }
}
