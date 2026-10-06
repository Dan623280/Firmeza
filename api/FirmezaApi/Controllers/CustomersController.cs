using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/customers"), Authorize(Roles = Roles.Administrator)]
public sealed class CustomersController(CustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerResponse>>> List([FromQuery] PaginationRequest page, CancellationToken ct)
    {
        return Ok(await service.ListAsync(page, ct));
    }
    [HttpGet("{id:guid}")] public async Task<ActionResult<CustomerResponse>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, User.IsInRole(Roles.Administrator), ct));
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = item.Id
        }, item);
    }
    [HttpPut("{id:guid}")] public async Task<ActionResult<CustomerResponse>> Update(Guid id, CustomerRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(id, request, ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }
}
