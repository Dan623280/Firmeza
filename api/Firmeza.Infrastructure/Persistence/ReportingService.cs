using Firmeza.Application.Reporting;
using Firmeza.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class ReportingService(FirmezaDbContext db) : IReportingService
{
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct) => new(
        await db.Products.CountAsync(x => x.IsActive, ct), await db.Customers.CountAsync(x => x.IsActive, ct), await db.Sales.CountAsync(ct),
        await db.Vehicles.CountAsync(x => x.IsActive, ct), await db.Rentals.CountAsync(ct),
        await db.Sales.Where(x => x.Status == SaleStatus.Completed).SumAsync(x => (decimal?)x.Total, ct) ?? 0);
}
