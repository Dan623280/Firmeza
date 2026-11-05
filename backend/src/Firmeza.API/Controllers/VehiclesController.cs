using System.Security.Claims;
using Firmeza.Application.DTOs;
using Firmeza.Application.Services;
using Firmeza.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.API.Controllers;

[ApiController, Route("api/vehiculos")]
public sealed class VehiclesController(CatalogService service) : ControllerBase
{
    [HttpGet, AllowAnonymous] public Task<List<VehicleDto>> List(CancellationToken ct) => service.Vehicles(User.IsInRole(Roles.Administrator), ct);
    [HttpPost, Authorize(Roles = Roles.Administrator)] public Task<VehicleDto> Create(VehicleInput input, CancellationToken ct) => service.SaveVehicle(null, input, ct);
    [HttpPut("{id:guid}"), Authorize(Roles = Roles.Administrator)] public Task<VehicleDto> Update(Guid id, VehicleInput input, CancellationToken ct) => service.SaveVehicle(id, input, ct);
    [HttpDelete("{id:guid}"), Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteVehicle(id, ct);
        return NoContent();
    }
}
[ApiController, Route("api/alquileres"), Authorize(Roles = Roles.Administrator + "," + Roles.Customer)]
public sealed class RentalsController(SalesService service) : ControllerBase
{
    string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; bool Admin => User.IsInRole(Roles.Administrator);
    [HttpGet] public Task<List<RentalDto>> List(CancellationToken ct) => service.Rentals(UserId, Admin, ct);
    [HttpPost] public Task<RentalDto> Create(RentalInput input, CancellationToken ct) => service.Rent(input, UserId, Admin, ct);
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await service.CancelRental(id, UserId, Admin, ct);
        return NoContent();
    }
}
