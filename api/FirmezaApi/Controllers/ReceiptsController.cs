using Firmeza.Application.Abstractions;
using Firmeza.Application.Authentication;
using Firmeza.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/sales"), Authorize(Roles = Roles.Administrator + "," + Roles.Customer)]
public sealed class ReceiptsController(SaleService sales, ICurrentCustomer current, IReceiptReader reader) : ControllerBase
{
    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        Guid? owner = User.IsInRole(Roles.Administrator) ? null : await current.GetRequiredCustomerIdAsync(ct);
        var sale = await sales.GetAsync(id, owner, ct);
        var stream = await reader.OpenAsync(id, ct);
        return File(stream, "application/pdf", sale.Number + ".pdf");
    }
}
