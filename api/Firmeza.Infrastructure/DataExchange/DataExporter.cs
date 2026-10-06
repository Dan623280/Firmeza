using System.Text.Json;
using AutoMapper;
using Firmeza.Application.Common;
using Firmeza.Application.Products;
using Firmeza.Application.Customers;
using Firmeza.Application.Sales;
using Firmeza.Application.Vehicles;
using Firmeza.Application.Rentals;
using Firmeza.Application.DataExchange;
using Firmeza.Infrastructure.Persistence;
using Firmeza.Infrastructure.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
namespace Firmeza.Infrastructure.DataExchange;

internal sealed class DataExporter(FirmezaDbContext db, IMapper mapper, IConfiguration config)
{
    public async Task<ExportFile> ExportAsync(string resource, string format, CancellationToken ct)
    {
        if (format is not ("xlsx" or "pdf"))
            throw new RequestException("invalid_format", "Format must be xlsx or pdf.");
        object records = resource.ToLowerInvariant() switch
        {
            "products" => mapper.Map<List<ProductResponse>>(await db.Products.AsNoTracking().OrderBy(x => x.Id).Take(10001).ToListAsync(ct)),
            "customers" => mapper.Map<List<CustomerResponse>>(await db.Customers.AsNoTracking().OrderBy(x => x.Id).Take(10001).ToListAsync(ct)),
            "sales" => mapper.Map<List<SaleResponse>>(await db.Sales.AsNoTracking().Include(x => x.Items).OrderBy(x => x.Id).Take(10001).ToListAsync(ct)),
            "vehicles" => mapper.Map<List<VehicleResponse>>(await db.Vehicles.AsNoTracking().OrderBy(x => x.Id).Take(10001).ToListAsync(ct)),
            "rentals" => mapper.Map<List<RentalResponse>>(await db.Rentals.AsNoTracking().OrderBy(x => x.Id).Take(10001).ToListAsync(ct)),
            _ => throw new RequestException("invalid_resource", "Resource must be products, customers, sales, vehicles, or rentals.")
        };
        var json = JsonSerializer.SerializeToElement(records, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (json.GetArrayLength() > 10000)
            throw new RequestException("export_too_large", "Export is limited to 10,000 records.", 413);
        var rows = json.EnumerateArray().ToList();
        var columns = rows.Count > 0 ? rows[0].EnumerateObject().Select(x => x.Name).ToList() : new List<string> { "id" };
        if (format == "xlsx" && resource == "sales")
            columns.Remove("items");
        string Value(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.String ? row.GetProperty(name).GetString() ?? "" : row.GetProperty(name).GetRawText();
        if (format == "xlsx")
        {
            ExcelLicense.Configure(config);
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Export");
            for (var c = 0; c < columns.Count; c++)
                sheet.Cells[1, c + 1].Value = columns[c];
            for (var r = 0; r < rows.Count; r++)
                for (var c = 0; c < columns.Count; c++)
                    sheet.Cells[r + 2, c + 1].Value = Value(rows[r], columns[c]);
            sheet.Cells[1, 1, 1, columns.Count].Style.Font.Bold = true;
            if (resource == "sales")
            {
                var details = package.Workbook.Worksheets.Add("SaleItems");
                var itemColumns = new[] { "saleId", "productId", "productName", "quantity", "unitPrice", "taxRate", "subtotal", "taxTotal" };
                for (var c = 0; c < itemColumns.Length; c++)
                    details.Cells[1, c + 1].Value = itemColumns[c];
                var rowIndex = 2;
                foreach (var sale in rows)
                    foreach (var item in sale.GetProperty("items").EnumerateArray())
                    {
                        details.Cells[rowIndex, 1].Value = sale.GetProperty("id").GetString();
                        for (var c = 1; c < itemColumns.Length; c++)
                            details.Cells[rowIndex, c + 1].Value = Value(item, itemColumns[c]);
                        rowIndex++;
                    }
                details.Cells[1, 1, 1, itemColumns.Length].Style.Font.Bold = true;
            }
            return new(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", resource + ".xlsx");
        }
        PdfReceiptGenerator.ConfigureLicense(config);
        var pdf = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(t => t.FontSize(7));
            page.Header().Text($"Firmeza — {resource} export").FontSize(16);
            page.Content().PaddingTop(10).Column(column =>
            {
                foreach (var row in rows)
                {
                    column.Item().BorderBottom(0.5f).PaddingVertical(5).Text(string.Join(" | ", columns.Select(c => $"{c}: {Value(row, c)}")));
                }
                if (rows.Count == 0)
                    column.Item().Text("No records.");
            });
        })).GeneratePdf();
        return new(pdf, "application/pdf", resource + ".pdf");
    }
}
