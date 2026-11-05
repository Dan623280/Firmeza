using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Firmeza.Domain.Entities;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Firmeza.Infrastructure.Services;

public sealed class ReceiptService(IHostEnvironment environment) : IReceiptService
{
    public async Task<FileDto> GenerateAsync(Sale sale, CancellationToken ct)
    {
        var bytes = Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(40);
            p.DefaultTextStyle(s => s.FontSize(11));
            p.Header().Text("FIRMEZA / Comprobante de compra").FontSize(22).Bold().FontColor(Colors.Orange.Darken3);
            p.Content().PaddingVertical(20).Column(c =>
            {
                c.Item().Text($"Venta {sale.Number} · {sale.Date:yyyy-MM-dd HH:mm} UTC · {sale.Status}");
                c.Item().Text($"Cliente: {sale.Customer.Name} {sale.Customer.LastName} · Documento: {sale.Customer.Document}");
                c.Item().Text(sale.Customer.Email);
                c.Item().Text(sale.Customer.Address);
                c.Item().PaddingVertical(20).Table(t =>
                {
                    t.ColumnsDefinition(cols => { cols.RelativeColumn(4); cols.RelativeColumn(); cols.RelativeColumn(2); cols.RelativeColumn(2); });
                    t.Header(h => { foreach (var v in new[] { "Producto", "Cant.", "Precio", "Subtotal" }) h.Cell().BorderBottom(1).Padding(5).Text(v).Bold(); });
                    foreach (var d in sale.Details)
                    {
                        foreach (var v in new[] { d.ProductName, d.Quantity.ToString(), d.UnitPrice.ToString("N2"), d.Subtotal.ToString("N2") })
                            t.Cell().Padding(5).Text(v);
                    }
                });
                c.Item().AlignRight().Text($"Subtotal: COP {sale.Subtotal:N2}\nIVA (19%): COP {sale.Tax:N2}\nTotal: COP {sale.Total:N2}").Bold();
            });
            p.Footer().AlignCenter().Text("Firmeza · Materiales y soluciones para construir");
        })).GeneratePdf();
        var name = $"{sale.Number}.pdf";
        var dir = Path.Combine(environment.ContentRootPath, "wwwroot", "recibos");
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(Path.Combine(dir, name), bytes, ct);
        return new(bytes, "application/pdf", name);
    }
}
