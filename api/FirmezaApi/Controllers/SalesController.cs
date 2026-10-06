using Firmeza.Application.Abstractions;
using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/sales"), Authorize(Roles = Roles.Administrator + "," + Roles.Customer)]
public sealed class SalesController(SaleService service, ICurrentCustomer current, IDeliveryQueue delivery) : ControllerBase
{
    private Task<Guid?> Owner(CancellationToken ct) => User.IsInRole(Roles.Administrator) ? Task.FromResult<Guid?>(null) : CustomerOwner(ct);
    private async Task<Guid?> CustomerOwner(CancellationToken ct) => await current.GetRequiredCustomerIdAsync(ct);
    [HttpGet] public async Task<ActionResult<PagedResult<SaleResponse>>> List([FromQuery] PaginationRequest page, CancellationToken ct) => Ok(await service.ListAsync(await Owner(ct), page, ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<SaleDeliveryResponse>> Get(Guid id, CancellationToken ct) => Ok(new SaleDeliveryResponse(await service.GetAsync(id, await Owner(ct), ct), await delivery.GetStatusAsync(id, ct)));
    [HttpPost, Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<SaleResponse>> Create(CreateSaleRequest request, [FromHeader(Name = "Idempotency-Key")] string key, CancellationToken ct)
    {
        var sale = await service.CreateAsync(await current.GetRequiredCustomerIdAsync(ct), request, key, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = sale.Id
        }, sale);
    }
    [HttpPost("for-customer/{customerId:guid}"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<SaleResponse>> CreateForCustomer(Guid customerId, CreateSaleRequest request, [FromHeader(Name = "Idempotency-Key")] string key, CancellationToken ct)
    {
        var sale = await service.CreateAsync(customerId, request, key, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = sale.Id
        }, sale);
    }
    [HttpPost("{id:guid}/cancel"), Authorize(Roles = Roles.Administrator)] public async Task<ActionResult<SaleResponse>> Cancel(Guid id, CancellationToken ct) => Ok(await service.CancelAsync(id, null, ct));
}
