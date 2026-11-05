using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Firmeza.Application.Services;
using Firmeza.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.API.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = Roles.Administrator)]
public sealed class AdminController(CatalogService catalog, IExchangeService exchange) : ControllerBase
{
    [HttpGet("dashboard")] public Task<DashboardDto> Dashboard(CancellationToken ct) => catalog.Dashboard(ct);
    [HttpPost("importar"), RequestSizeLimit(10_000_000)]
    public async Task<ImportResult> Import(IFormFile file, CancellationToken ct)
    {
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new Firmeza.Domain.Exceptions.BusinessException("Selecciona un archivo .xlsx.");
        using var stream = file.OpenReadStream();
        try
        {
            return await exchange.ImportAsync(stream, ct);
        }
        catch (System.IO.InvalidDataException) { throw new Firmeza.Domain.Exceptions.BusinessException("El archivo no es un Excel válido."); }
    }
    [HttpGet("exportar/{resource}/{format}")]
    public async Task<IActionResult> Export(string resource, string format, CancellationToken ct)
    {
        var f = await exchange.ExportAsync(resource, format, ct);
        return File(f.Content, f.ContentType, f.Name);
    }
}
