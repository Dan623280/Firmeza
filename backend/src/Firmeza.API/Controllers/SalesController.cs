using System.Security.Claims;
using AutoMapper;
using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Firmeza.Application.Services;
using Firmeza.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.API.Controllers;

[ApiController, Route("api/ventas"), Authorize(Roles = Roles.Administrator + "," + Roles.Customer)]
public sealed class SalesController(SalesService service, IReceiptService receipts, IMapper mapper) : ControllerBase
{
    string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; bool Admin => User.IsInRole(Roles.Administrator);
    [HttpGet] public Task<List<SaleDto>> List(CancellationToken ct) => service.List(UserId, Admin, ct);
    [HttpGet("{id:guid}")] public async Task<SaleDto> Get(Guid id, CancellationToken ct) => mapper.Map<SaleDto>(await service.Get(id, UserId, Admin, ct));
    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create(SaleInput input, CancellationToken ct)
    {
        var s = await service.Create(input, UserId, Admin, ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = s.Id
        }, s);
    }
    [HttpPut("{id:guid}/anular"), Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await service.Cancel(id, ct);
        return NoContent();
    }
    [HttpDelete("{id:guid}"), Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.Cancel(id, ct);
        return NoContent();
    }
    [HttpGet("{id:guid}/comprobante")]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct)
    {
        var f = await receipts.GenerateAsync(await service.Get(id, UserId, Admin, ct), ct);
        return File(f.Content, f.ContentType, f.Name);
    }
    [HttpPost("{id:guid}/reenviar")]
    public async Task<object> Resend(Guid id, CancellationToken ct)
    {
        var s = await service.Get(id, UserId, Admin, ct);
        await service.Deliver(s, ct);
        return new
        {
            s.EmailSent
        };
    }
}
