using Firmeza.Application.Abstractions;
using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Application.Rentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/rentals"), Authorize(Roles = Roles.Administrator + "," + Roles.Customer)]
public sealed class RentalsController(RentalService service, ICurrentCustomer current) : ControllerBase
{
    private async Task<Guid?> Owner(CancellationToken ct) => User.IsInRole(Roles.Administrator) ? null : await current.GetRequiredCustomerIdAsync(ct);
    [HttpGet] public async Task<ActionResult<PagedResult<RentalResponse>>> List([FromQuery] PaginationRequest page, CancellationToken ct) => Ok(await service.ListAsync(await Owner(ct), page, ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<RentalResponse>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id, await Owner(ct), ct));
    [HttpPost, Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<RentalResponse>> Create(CreateRentalRequest request, CancellationToken ct)
    {
        var rental = await service.CreateAsync(await current.GetRequiredCustomerIdAsync(ct), request, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = rental.Id
        }, rental);
    }
    [HttpPost("for-customer/{customerId:guid}"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<RentalResponse>> CreateForCustomer(Guid customerId, CreateRentalRequest request, CancellationToken ct)
    {
        var rental = await service.CreateAsync(customerId, request, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = rental.Id
        }, rental);
    }
    [HttpPost("{id:guid}/cancel")] public async Task<ActionResult<RentalResponse>> Cancel(Guid id, CancellationToken ct) => Ok(await service.TransitionAsync(id, await Owner(ct), false, ct));
    [HttpPost("{id:guid}/complete"), Authorize(Roles = Roles.Administrator)] public async Task<ActionResult<RentalResponse>> Complete(Guid id, CancellationToken ct) => Ok(await service.TransitionAsync(id, null, true, ct));
}
