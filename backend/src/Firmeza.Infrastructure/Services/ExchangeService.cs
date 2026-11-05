using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Firmeza.Application.Services;
using Firmeza.Domain.Exceptions;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
namespace Firmeza.Infrastructure.Services;

public sealed class ExchangeService(IUnitOfWork db, CatalogService catalog, SalesService sales) : IExchangeService
{
    static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))).ToLowerInvariant();
    static void Validate(object input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new BusinessException(string.Join("; ", errors.Select(e => e.ErrorMessage)));
    }
    public async Task<ImportResult> ImportAsync(Stream stream, CancellationToken ct)
    {
        using var package = new ExcelPackage(stream);
        int products = 0, customers = 0, saleCount = 0;
        List<string> errors = [];
        // Cabeceras prefijadas permiten mezclar varias entidades en una misma fila.
        foreach (var sheet in package.Workbook.Worksheets)
        {
            if (sheet.Dimension == null)
                continue;
            if (sheet.Dimension.End.Row > 5001)
                throw new BusinessException("Máximo 5000 filas por hoja.");
            var cols = new Dictionary<string, int>();
            for (int col = 1; col <= sheet.Dimension.End.Column; col++)
                cols.TryAdd(Normalize(sheet.Cells[1, col].Text), col);
            string Cell(int row, params string[] keys)
            {
                foreach (var key in keys)
                    if (cols.TryGetValue(key, out var col))
                        return sheet.Cells[row, col].Text.Trim();
                return "";
            }
            decimal Decimal(int row, params string[] keys)
            {
                var cell = keys.Select(k => cols.GetValueOrDefault(k)).FirstOrDefault(c => c > 0);
                if (cell > 0 && sheet.Cells[row, cell].Value is double d)
                    return (decimal)d;
                var text = Cell(row, keys);
                if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
                    throw new BusinessException($"Número inválido: {text}. Usa punto decimal sin separador de miles.");
                return result;
            }
            for (int row = 2; row <= sheet.Dimension.End.Row; row++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var name = Cell(row, "productonombre", "producto", "nombreproducto");
                    var document = Cell(row, "clientedocumento", "documento");
                    if (name == "" && document == "")
                        continue;
                    Guid? customerId = null;
                    Guid? productId = null;
                    if (document != "")
                    {
                        var input = new CustomerInput(document, Cell(row, "clientenombre", "nombrecliente"), Cell(row, "clienteapellido", "apellido"), Cell(row, "clientecorreo", "correo", "email"), Cell(row, "clientetelefono", "telefono"), Cell(row, "clientedireccion", "direccion"), checked((int)Decimal(row, "clienteedad", "edad")));
                        Validate(input);
                        if (Decimal(row, "clienteedad", "edad") != input.Age)
                            throw new BusinessException("La edad debe ser un entero.");
                        var existing = (await db.Customers.ListAsync(ct)).FirstOrDefault(c => c.Document == document);
                        input = input with
                        {
                            Active = existing?.Active ?? true
                        };
                        var customer = await catalog.SaveCustomer(existing?.Id, input, ct);
                        customerId = customer.Id;
                        customers++;
                    }
                    if (name != "")
                    {
                        var stock = Decimal(row, "productostock", "stock");
                        if (stock != decimal.Truncate(stock))
                            throw new BusinessException("Stock debe ser entero.");
                        var input = new ProductInput(name, Cell(row, "productodescripcion", "descripcion"), Decimal(row, "productoprecio", "precio"), checked((int)stock));
                        Validate(input);
                        var existing = (await db.Products.ListAsync(ct)).FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        input = input with
                        {
                            Active = existing?.Active ?? true,
                            Description = cols.ContainsKey("productodescripcion") || cols.ContainsKey("descripcion") ? input.Description : existing?.Description ?? ""
                        };
                        var product = await catalog.SaveProduct(existing?.Id, input, ct);
                        productId = product.Id;
                        products++;
                    }
                    var quantityText = Cell(row, "ventacantidad", "cantidad");
                    if (quantityText != "")
                    {
                        if (customerId == null || productId == null)
                            throw new BusinessException("Una venta necesita producto y cliente en la misma fila.");
                        if (!int.TryParse(quantityText, out var quantity) || quantity <= 0)
                            throw new BusinessException("Cantidad inválida.");
                        await sales.Create(new SaleInput(customerId, [new(productId.Value, quantity)]), "", true, ct);
                        saleCount++;
                    }
                }
                catch (Exception ex) when (ex is BusinessException or ValidationException or FormatException or OverflowException) { errors.Add($"{sheet.Name}, fila {row}: {ex.Message}"); }
            }
        }
        return new(products, customers, saleCount, errors);
    }
    public async Task<FileDto> ExportAsync(string resource, string format, CancellationToken ct)
    {
        List<string[]> rows = resource switch
        {
            "productos" => (await db.Products.ListAsync(ct)).Select(p => new[] { p.Id.ToString(), p.Name, p.Description, p.Price.ToString(CultureInfo.InvariantCulture), p.Stock.ToString() }).Prepend(["Id", "ProductoNombre", "ProductoDescripcion", "ProductoPrecio", "ProductoStock"]).ToList(),
            "clientes" => (await db.Customers.ListAsync(ct)).Select(c => new[] { c.Document, c.Name, c.LastName, c.Email, c.Phone, c.Address, c.Age.ToString() }).Prepend(["ClienteDocumento", "ClienteNombre", "ClienteApellido", "ClienteCorreo", "ClienteTelefono", "ClienteDireccion", "ClienteEdad"]).ToList(),
            "ventas" => (await db.SalesAsync(ct)).Select(s => new[] { s.Number, s.Customer.Name, s.Date.ToString("yyyy-MM-dd"), s.Subtotal.ToString(CultureInfo.InvariantCulture), s.Tax.ToString(CultureInfo.InvariantCulture), s.Total.ToString(CultureInfo.InvariantCulture), s.Status.ToString() }).Prepend(["Número", "Cliente", "Fecha", "Subtotal", "IVA", "Total", "Estado"]).ToList(),
            _ => throw new BusinessException("Recurso inválido.")
        };
        if (format == "xlsx")
        {
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add(resource);
            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    sheet.Cells[r + 1, c + 1].Value = rows[r][c];
            sheet.Cells[1, 1, 1, rows[0].Length].Style.Font.Bold = true;
            sheet.Cells.AutoFitColumns();
            return new(await package.GetAsByteArrayAsync(ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{resource}.xlsx");
        }
        if (format != "pdf")
            throw new BusinessException("Formato inválido.");
        var pdf = Document.Create(doc => doc.Page(p => { p.Size(PageSizes.A4.Landscape()); p.Margin(24); p.Header().Text($"FIRMEZA · {resource}").FontSize(20); p.Content().PaddingTop(15).Table(t => { t.ColumnsDefinition(c => { for (int i = 0; i < rows[0].Length; i++) c.RelativeColumn(); }); t.Header(h => { foreach (var v in rows[0]) h.Cell().Padding(4).Text(v).Bold(); }); foreach (var row in rows.Skip(1)) foreach (var v in row) t.Cell().BorderBottom(0.2f).Padding(4).Text(v).FontSize(9); }); })).GeneratePdf();
        return new(pdf, "application/pdf", $"{resource}.pdf");
    }
}
