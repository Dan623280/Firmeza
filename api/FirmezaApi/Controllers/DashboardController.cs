using Firmeza.Application.Authentication;
using Firmeza.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/dashboard"), Authorize(Roles = Roles.Administrator)]
public sealed class DashboardController(IReportingService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct) => Ok(await service.GetDashboardAsync(ct));
}
