using System.Globalization;
using System.IO.Compression;
using Firmeza.Application.Common;
using Firmeza.Application.Customers;
using Firmeza.Application.Products;
using Firmeza.Application.DataExchange;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Firmeza.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
namespace Firmeza.Infrastructure.DataExchange;

internal sealed class ExcelReader(IConfiguration config)
{
    public async Task<(List<ImportRecord> Records, List<ImportError> Errors, string Hash)> ReadAsync(Stream stream, CancellationToken ct)
    {
        ExcelLicense.Configure(config);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > 10 * 1024 * 1024)
                throw new RequestException("file_too_large", "Excel upload must not exceed 10 MiB.", 413);
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        buffer.Position = 0;
        try
        {
            using var normalized = new MemoryStream();
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Read, true))
            using (var safeZip = new ZipArchive(normalized, ZipArchiveMode.Create, true))
            {
                if (zip.Entries.Count > 5000 || zip.Entries.Sum(x => x.Length) > 50 * 1024 * 1024)
                    throw new RequestException("file_too_large", "Workbook expanded size exceeds 50 MiB.", 413);
                if (zip.Entries.Select(x => x.FullName).Distinct(StringComparer.Ordinal).Count() != zip.Entries.Count)
                    throw new RequestException("invalid_workbook", "Workbook contains duplicate ZIP members.");
                long expanded = 0;
                foreach (var entry in zip.Entries)
                {
                    await using var contents = entry.Open();
                    long entryBytes = 0;
                    await using var output = safeZip.CreateEntry(entry.FullName, System.IO.Compression.CompressionLevel.Fastest).Open();
                    var crc = new System.IO.Hashing.Crc32();
                    while ((read = await contents.ReadAsync(chunk, ct)) > 0)
                    {
                        expanded += read;
                        entryBytes += read;
                        crc.Append(chunk.AsSpan(0, read));
                        if (expanded > 50 * 1024 * 1024)
                            throw new RequestException("file_too_large", "Workbook expanded size exceeds 50 MiB.", 413);
                        await output.WriteAsync(chunk.AsMemory(0, read), ct);
                    }
                    if (crc.GetCurrentHashAsUInt32() != entry.Crc32)
                        throw new RequestException("invalid_workbook", "Workbook ZIP integrity check failed.");
                    if (entryBytes != entry.Length)
                        throw new RequestException("invalid_workbook", "Workbook ZIP metadata is inconsistent.");
                }
            }
            normalized.Position = 0;
            using var package = new ExcelPackage(normalized);
            var sheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == "Records") ?? throw new RequestException("invalid_workbook", "Workbook must contain a Records sheet.");
            if (sheet.Dimension is null)
                throw new RequestException("invalid_workbook", "Workbook is empty.");
            if (sheet.Dimension.End.Row > 10001 || sheet.Dimension.End.Column > 18)
                throw new RequestException("file_too_large", "Workbook is limited to 10,000 data rows and 18 columns.", 413);
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= sheet.Dimension.End.Column; c++)
                if (!columns.TryAdd(sheet.Cells[1, c].Text.Trim(), c))
                    throw new RequestException("invalid_workbook", "Workbook has duplicate column headings.");
            var required = new[] { "Type", "Reference", "Id", "Name", "Description", "Price", "Stock", "TaxRate", "DocumentNumber", "FirstName", "LastName", "Email", "Phone", "Address", "CustomerReference", "ProductReference", "Quantity", "IdempotencyKey" };
            if (required.Any(x => !columns.ContainsKey(x)))
                throw new RequestException("invalid_workbook", "Use the documented import template with all 18 column headings.");
            var records = new List<ImportRecord>();
            var errors = new List<ImportError>();
            for (var r = 2; r <= sheet.Dimension.End.Row; r++)
            {
                ct.ThrowIfCancellationRequested();
                string Cell(string name) => sheet.Cells[r, columns[name]].Text.Trim();
                if (required.All(x => Cell(x) == ""))
                    continue;
                try
                {
                    if (required.Any(x => !string.IsNullOrEmpty(sheet.Cells[r, columns[x]].Formula)))
                        throw new RequestException("invalid_formula", "Import cells must contain literal values, not formulas.");
                    var type = Cell("Type");
                    var reference = Cell("Reference");
                    if (reference.Length is < 1 or > 100)
                        throw new RequestException("invalid_reference", "Reference must contain 1–100 characters.");
                    Guid? id = string.IsNullOrEmpty(Cell("Id")) ? null : Guid.Parse(Cell("Id"));
                    if (id == Guid.Empty)
                        throw new RequestException("invalid_id", "ID must not be empty.");
                    decimal Decimal(string name) => decimal.Parse(Cell(name), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                    int Integer(string name) => int.Parse(Cell(name), NumberStyles.Integer, CultureInfo.InvariantCulture);
                    ProductRequest? product = null;
                    CustomerRequest? customer = null;
                    var quantity = 0;
                    switch (type.ToLowerInvariant())
                    {
                        case "product":
                            product = new(Cell("Name"), Cell("Description"), Decimal("Price"), Integer("Stock"), Decimal("TaxRate"));
                            Product.Create(product.Name, product.Description, Money.Create(product.Price), product.Stock, product.TaxRate);
                            break;
                        case "customer":
                            customer = new(Cell("DocumentNumber"), Cell("FirstName"), Cell("LastName"), Cell("Email"), Cell("Phone"), Cell("Address"));
                            Customer.Create(customer.DocumentNumber, customer.FirstName, customer.LastName, customer.Email, customer.Phone, customer.Address);
                            break;
                        case "sale":
                            quantity = Integer("Quantity");
                            if (quantity <= 0 || Cell("CustomerReference") == "" || Cell("ProductReference") == "" || Cell("IdempotencyKey").Length is < 1 or > 100)
                                throw new RequestException("invalid_sale", "Sale requires references, positive quantity and a 1–100 character idempotency key.");
                            if (id.HasValue)
                                throw new RequestException("invalid_sale", "Posted sales cannot be updated by ID.");
                            break;
                        default:
                            throw new RequestException("invalid_type", "Type must be Product, Customer, or Sale.");
                    }
                    records.Add(new(r, type.ToLowerInvariant(), reference, id, product, customer, Cell("CustomerReference"), Cell("ProductReference"), quantity, Cell("IdempotencyKey")));
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or DomainException or RequestException) { errors.Add(new(r, "invalid_row", ex.Message)); }
            }
            foreach (var duplicate in records.Where(x => x.Type != "sale").GroupBy(x => (x.Type, x.Reference)).Where(x => x.Count() > 1))
                foreach (var row in duplicate)
                    errors.Add(new(row.Row, "duplicate_reference", "Reference must be unique within its record type."));
            foreach (var sale in records.Where(x => x.Type == "sale").GroupBy(x => x.Reference))
            {
                if (sale.Select(x => x.CustomerReference).Distinct().Count() != 1 || sale.Select(x => x.IdempotencyKey).Distinct().Count() != 1 || sale.Count() > 100 || sale.Select(x => x.ProductReference).Distinct().Count() != sale.Count())
                    errors.Add(new(sale.First().Row, "invalid_sale", "A sale group requires one customer, one idempotency key and at most 100 distinct products."));
            }
            return (records, errors, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray())));
        }
        catch (InvalidDataException) { throw new RequestException("invalid_workbook", "File is not a valid XLSX workbook."); }
        catch (Exception ex) when (ex is IOException or System.Xml.XmlException or ArgumentException or OfficeOpenXml.Packaging.Ionic.Zip.ZipException) { throw new RequestException("invalid_workbook", "Workbook cannot be read."); }
    }
}
