using AutoMapper;
using Firmeza.Application.DTOs;
using Firmeza.Application.Exceptions;
using Firmeza.Application.Interfaces;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Exceptions;
namespace Firmeza.Application.Services;

public sealed class CatalogService(IUnitOfWork db, IMapper mapper)
{
    public async Task<List<ProductDto>> Products(string? search, bool admin, CancellationToken ct) => mapper.Map<List<ProductDto>>((await db.Products.ListAsync(ct)).Where(p => (admin || p.Active) && (string.IsNullOrWhiteSpace(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))).OrderBy(p => p.Name));
    public async Task<ProductDto> SaveProduct(Guid? id, ProductInput input, CancellationToken ct)
    {
        var p = id.HasValue ? await db.Products.FindAsync(id.Value, ct) ?? throw new NotFoundException("el producto") : new Product();
        mapper.Map(input, p);
        p.UpdatedAt = DateTime.UtcNow;
        if (!id.HasValue)
            db.Products.Add(p);
        await db.SaveAsync(ct);
        return mapper.Map<ProductDto>(p);
    }
    public async Task DeleteProduct(Guid id, CancellationToken ct)
    {
        var p = await db.Products.FindAsync(id, ct) ?? throw new NotFoundException("el producto");
        p.Active = false;
        await db.SaveAsync(ct);
    }
    public async Task<List<CustomerDto>> Customers(string? search, CancellationToken ct) => mapper.Map<List<CustomerDto>>((await db.Customers.ListAsync(ct)).Where(c => string.IsNullOrWhiteSpace(search) || ($"{c.Name} {c.LastName} {c.Document}").Contains(search, StringComparison.OrdinalIgnoreCase)));
    public async Task<CustomerDto> SaveCustomer(Guid? id, CustomerInput input, CancellationToken ct)
    {
        if ((await db.Customers.ListAsync(ct)).Any(c => c.Id != id && (c.Document == input.Document || c.Email == input.Email)))
            throw new BusinessException("Ya existe un cliente con ese documento o correo.", 409);
        var c = id.HasValue ? await db.Customers.FindAsync(id.Value, ct) ?? throw new NotFoundException("el cliente") : new Customer();
        // El correo de cuentas registradas se mantiene sincronizado con Identity.
        if (c.UserId != null && c.Email != input.Email)
            throw new BusinessException("No se puede cambiar el correo de una cuenta registrada desde este módulo.");
        mapper.Map(input, c);
        if (!id.HasValue)
            db.Customers.Add(c);
        await db.SaveAsync(ct);
        return mapper.Map<CustomerDto>(c);
    }
    public async Task DeleteCustomer(Guid id, CancellationToken ct)
    {
        var c = await db.Customers.FindAsync(id, ct) ?? throw new NotFoundException("el cliente");
        c.Active = false;
        await db.SaveAsync(ct);
    }
    public async Task<List<VehicleDto>> Vehicles(bool admin, CancellationToken ct) => mapper.Map<List<VehicleDto>>((await db.Vehicles.ListAsync(ct)).Where(v => admin || v.Active));
    public async Task<VehicleDto> SaveVehicle(Guid? id, VehicleInput input, CancellationToken ct)
    {
        var v = id.HasValue ? await db.Vehicles.FindAsync(id.Value, ct) ?? throw new NotFoundException("el vehículo") : new Vehicle();
        mapper.Map(input, v);
        if (!id.HasValue)
            db.Vehicles.Add(v);
        await db.SaveAsync(ct);
        return mapper.Map<VehicleDto>(v);
    }
    public async Task DeleteVehicle(Guid id, CancellationToken ct)
    {
        var v = await db.Vehicles.FindAsync(id, ct) ?? throw new NotFoundException("el vehículo");
        v.Active = false;
        await db.SaveAsync(ct);
    }
    public async Task<DashboardDto> Dashboard(CancellationToken ct)
    {
        var sales = await db.SalesAsync(ct);
        return new((await db.Products.ListAsync(ct)).Count, (await db.Customers.ListAsync(ct)).Count, sales.Count, sales.Where(s => s.Status == Domain.Enums.SaleStatus.Confirmada).Sum(s => s.Total), (await db.RentalsAsync(ct)).Count);
    }
}
