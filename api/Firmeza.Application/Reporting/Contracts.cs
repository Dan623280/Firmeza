namespace Firmeza.Application.Reporting;

public sealed record DashboardResponse(int Products, int Customers, int Sales, int Vehicles, int Rentals, decimal SalesTotal);
public interface IReportingService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct);
}
