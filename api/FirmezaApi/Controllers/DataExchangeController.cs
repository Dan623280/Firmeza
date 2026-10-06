using Firmeza.Application.Authentication;
using Firmeza.Application.Common;
using Firmeza.Application.DataExchange;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/data"), Authorize(Roles = Roles.Administrator)]
public sealed class DataExchangeController(IDataExchangeService service) : ControllerBase
{
    [HttpPost("import"), RequestSizeLimit(11 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<ActionResult<ImportReport>> Import(IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > 10 * 1024 * 1024 || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new RequestException("invalid_upload", "Upload an XLSX file of at most 10 MiB.");
        await using var stream = file.OpenReadStream();
        return Ok(await service.ImportAsync(stream, ct));
    }
    [HttpGet("export/{resource}")]
    public async Task<IActionResult> Export(string resource, [FromQuery] string format = "xlsx", CancellationToken ct = default)
    {
        var file = await service.ExportAsync(resource.ToLowerInvariant(), format.ToLowerInvariant(), ct);
        return File(file.Contents, file.ContentType, file.FileName);
    }
}
