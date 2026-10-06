using AutoMapper;
using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Application.Rentals;

public sealed class RentalService(IRentalRepository rentals, IVehicleRepository vehicles, ICustomerRepository customers, IBusinessTransaction transaction, IUnitOfWork work, IMapper mapper)
{
    public Task<RentalResponse> CreateAsync(Guid customerId, CreateRentalRequest request, CancellationToken ct)
    {
        var period = RentalPeriod.Create(request.StartDate, request.EndDate);
        return transaction.ExecuteAsync(async token =>
        {
            var customer = await customers.GetAsync(customerId, true, token) ?? throw RequestException.NotFound();
            if (!customer.IsActive)
                throw RequestException.Conflict("Customer is inactive.");
            var vehicle = await vehicles.GetAsync(request.VehicleId, true, token) ?? throw RequestException.NotFound();
            if (!vehicle.IsActive)
                throw RequestException.Conflict("Vehicle is inactive.");
            if (await rentals.OverlapsAsync(vehicle.Id, period.Start, period.End, token))
                throw RequestException.Conflict("Vehicle is already reserved for these dates.");
            var rental = Rental.Create(customerId, vehicle.Id, period, Money.Create(vehicle.DailyPrice));
            rentals.Add(rental);
            await work.SaveChangesAsync(token);
            return mapper.Map<RentalResponse>(rental);
        }, ct);
    }
    public async Task<RentalResponse> GetAsync(Guid id, Guid? owner, CancellationToken ct) => mapper.Map<RentalResponse>(await OwnedAsync(id, owner, false, ct));
    public async Task<PagedResult<RentalResponse>> ListAsync(Guid? owner, PaginationRequest page, CancellationToken ct)
    {
        page.Validate();
        var result = await rentals.ListAsync(owner, page, ct);
        return new(mapper.Map<List<RentalResponse>>(result.Items), result.Total, result.Page, result.PageSize);
    }
    public Task<RentalResponse> TransitionAsync(Guid id, Guid? owner, bool complete, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        var rental = await OwnedAsync(id, owner, true, token);
        if (complete)
            rental.Complete();
        else
            rental.Cancel();
        await work.SaveChangesAsync(token);
        return mapper.Map<RentalResponse>(rental);
    }, ct);
    private async Task<Rental> OwnedAsync(Guid id, Guid? owner, bool locked, CancellationToken ct)
    {
        var rental = await rentals.GetAsync(id, locked, ct) ?? throw RequestException.NotFound();
        if (owner.HasValue && rental.CustomerId != owner)
            throw RequestException.NotFound();
        return rental;
    }
}
