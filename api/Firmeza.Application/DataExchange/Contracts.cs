namespace Firmeza.Application.DataExchange;

public sealed record ImportError(int Row, string Code, string Message);
public sealed record ImportReport(int AppliedRows, IReadOnlyList<ImportError> Errors);
public sealed record ExportFile(byte[] Contents, string ContentType, string FileName);
public interface IDataExchangeService
{
    Task<ImportReport> ImportAsync(Stream workbook, CancellationToken ct);
    Task<ExportFile> ExportAsync(string resource, string format, CancellationToken ct);
}
