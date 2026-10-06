using Firmeza.Application.DataExchange;
namespace Firmeza.Infrastructure.DataExchange;

internal sealed class DataExchangeService(ExcelImporter importer, DataExporter exporter) : IDataExchangeService
{
    public Task<ImportReport> ImportAsync(Stream workbook, CancellationToken ct) => importer.ImportAsync(workbook, ct);
    public Task<ExportFile> ExportAsync(string resource, string format, CancellationToken ct) => exporter.ExportAsync(resource, format, ct);
}
