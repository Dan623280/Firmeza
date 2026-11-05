using AutoMapper;
using Microsoft.Extensions.Logging;
using Firmeza.Application.DTOs;
using Firmeza.Application.Exceptions;
using Firmeza.Application.Interfaces;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Application.Services;

public sealed class SalesService(IUnitOfWork db, IMapper mapper, IReceiptService receipts, IEmailService email, ILogger<SalesService>? logger = null)
{
    async Task<Customer> Customer(string userId, Guid? customerId, bool admin, CancellationToken ct) =>
     (await db.Customers.ListAsync(ct)).FirstOrDefault(c => c.Active && (admin ? c.Id == customerId : c.UserId == userId)) ?? throw new BusinessException("Cliente no disponible.", 403);
    public async Task<List<SaleDto>> List(string userId, bool admin, CancellationToken ct) => mapper.Map<List<SaleDto>>((await db.SalesAsync(ct)).Where(s => admin || s.Customer.UserId == userId).OrderByDescending(s => s.Date));
    public async Task<Sale> Get(Guid id, string userId, bool admin, CancellationToken ct) => (await db.SalesAsync(ct)).FirstOrDefault(s => s.Id == id && (admin || s.Customer.UserId == userId)) ?? throw new NotFoundException("la venta");
    public async Task<SaleDto> Create(SaleInput input, string userId, bool admin, CancellationToken ct)
    {
        if (input.Items.Count == 0 || input.Items.Any(i => i.Quantity <= 0))
            throw new BusinessException("Agrega productos con cantidades positivas.");
        Sale sale;
        await using (var tx = await db.BeginAsync(ct))
        {
            var customer = await Customer(userId, input.CustomerId, admin, ct);
            sale = new Sale { CustomerId = customer.Id, Customer = customer };
            foreach (var line in input.Items.GroupBy(i => i.ProductId))
            {
                var quantity = checked(line.Sum(i => i.Quantity));
                var p = await db.Products.FindAsync(line.Key, ct) ?? throw new NotFoundException("el producto");
                p.RemoveStock(quantity);
                sale.Details.Add(new SaleDetail { ProductId = p.Id, Product = p, ProductName = p.Name, Quantity = quantity, UnitPrice = p.Price, Subtotal = Money.Round(p.Price * quantity) });
            }
            sale.CalculateTotals();
            db.AddSale(sale);
            await db.SaveAsync(ct);
            await tx.CommitAsync(ct);
        }
        await Deliver(sale, ct);
        return mapper.Map<SaleDto>(sale);
    }
    public async Task Deliver(Sale sale, CancellationToken ct)
    {
        // La compra ya está confirmada; un fallo de correo no revierte stock ni duplica la venta.
        try
        {
            var pdf = await receipts.GenerateAsync(sale, ct);
            sale.ReceiptPath = pdf.Name;
            sale.EmailSent = await email.SendReceiptAsync(sale.Customer.Email, sale.Number, pdf, ct);
            await db.SaveAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger?.LogError(ex, "No se pudo preparar o enviar el comprobante {Number}", sale.Number); sale.EmailSent = false; await db.SaveAsync(ct); }
    }
    public async Task Cancel(Guid id, CancellationToken ct)
    {
        await using var tx = await db.BeginAsync(ct);
        var s = await Get(id, "", true, ct);
        if (s.Status == SaleStatus.Anulada)
            throw new BusinessException("La venta ya fue anulada.", 409);
        foreach (var d in s.Details)
        {
            var p = await db.Products.FindAsync(d.ProductId, ct) ?? throw new NotFoundException("el producto");
            p.Stock = checked(p.Stock + d.Quantity);
        }
        s.Status = SaleStatus.Anulada;
        await db.SaveAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task<List<RentalDto>> Rentals(string userId, bool admin, CancellationToken ct) => mapper.Map<List<RentalDto>>((await db.RentalsAsync(ct)).Where(r => admin || r.Customer.UserId == userId));
    public async Task<RentalDto> Rent(RentalInput input, string userId, bool admin, CancellationToken ct)
    {
        if (input.StartDate < DateOnly.FromDateTime(DateTime.UtcNow) || input.EndDate <= input.StartDate)
            throw new BusinessException("Elige fechas futuras y una devolución posterior al inicio.");
        await using var tx = await db.BeginAsync(ct);
        var customer = await Customer(userId, input.CustomerId, admin, ct);
        var v = await db.Vehicles.FindAsync(input.VehicleId, ct) ?? throw new NotFoundException("el vehículo");
        if (!v.Active || !v.Available || (await db.RentalsAsync(ct)).Any(r => r.VehicleId == v.Id && r.Status == RentalStatus.Reservado && r.StartDate < input.EndDate && r.EndDate > input.StartDate))
            throw new BusinessException("Vehículo no disponible para esas fechas.", 409);
        var rental = new Rental { Customer = customer, CustomerId = customer.Id, Vehicle = v, VehicleId = v.Id, StartDate = input.StartDate, EndDate = input.EndDate, PricePerDay = v.PricePerDay, Total = Money.Round(v.PricePerDay * (input.EndDate.DayNumber - input.StartDate.DayNumber)) };
        db.AddRental(rental);
        await db.SaveAsync(ct);
        await tx.CommitAsync(ct);
        return mapper.Map<RentalDto>(rental);
    }
    public async Task CancelRental(Guid id, string userId, bool admin, CancellationToken ct)
    {
        var r = (await db.RentalsAsync(ct)).FirstOrDefault(r => r.Id == id && (admin || r.Customer.UserId == userId)) ?? throw new NotFoundException("el alquiler");
        r.Status = RentalStatus.Cancelado;
        await db.SaveAsync(ct);
    }
}
