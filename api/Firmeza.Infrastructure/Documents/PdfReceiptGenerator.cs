using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using Firmeza.Application.Customers;
using Firmeza.Application.Sales;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Firmeza.Infrastructure.Documents;

public sealed class PdfReceiptGenerator(IConfiguration config) : IReceiptGenerator
{
    internal static void ConfigureLicense(IConfiguration config)
    {
        if (!Enum.TryParse<LicenseType>(config["Licenses:QuestPdf"], true, out var license))
            throw new RequestException("pdf_not_configured", "Configure Licenses__QuestPdf according to your eligibility.", 503);
        QuestPDF.Settings.License = license;
    }
    public Task<byte[]> GenerateAsync(SaleResponse sale, CustomerResponse customer, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ConfigureLicense(config);
        var pdf = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Text("FERRETERIA FIRMEZA — Purchase Receipt").FontSize(18).Bold();
            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Item().Text($"Sale: {sale.Number} | Date: {sale.CreatedAt:yyyy-MM-dd HH:mm} UTC | Status: {sale.Status}");
                column.Item().Text($"Customer: {customer.FirstName} {customer.LastName} | Document: {customer.DocumentNumber}");
                column.Item().Text($"Email: {customer.Email} | Address: {customer.Address}");
                column.Item().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); c.RelativeColumn(2); c.RelativeColumn(2); });
                    table.Header(h => { h.Cell().Text("Product").Bold(); h.Cell().Text("Quantity").Bold(); h.Cell().Text("Unit price").Bold(); h.Cell().Text("Subtotal").Bold(); });
                    foreach (var line in sale.Items)
                    {
                        table.Cell().Text(line.ProductName);
                        table.Cell().Text(line.Quantity.ToString());
                        table.Cell().Text($"{line.UnitPrice:N2} COP");
                        table.Cell().Text($"{line.Subtotal:N2} COP");
                    }
                });
                column.Item().Text($"Subtotal: {sale.Subtotal:N2} COP");
                column.Item().Text($"Tax: {sale.TaxTotal:N2} COP");
                column.Item().Text($"Total: {sale.Total:N2} COP").Bold();
            });
            page.Footer().Text("Purchase receipt. This document does not replace a regulated electronic tax invoice.");
        })).GeneratePdf();
        return Task.FromResult(pdf);
    }
}
