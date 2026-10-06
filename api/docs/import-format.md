# Workbook import and exports

Import endpoint: `POST /api/v1/data/import` with multipart field `file`. Administrative access and a configured EPPlus license are required. The workbook must be XLSX and have a sheet named `Records`.

The header row must contain all these 18 unique headings; their order may vary and matching is case insensitive:

```text
Type, Reference, Id, Name, Description, Price, Stock, TaxRate, DocumentNumber,
FirstName, LastName, Email, Phone, Address, CustomerReference,
ProductReference, Quantity, IdempotencyKey
```

Decimal text uses an invariant decimal point and no thousands separators. Cells must be literal values, never formulas. Empty rows are ignored. Dates are not imported here; vehicle rentals use the REST endpoints.

| Type | Required values | Meaning |
| --- | --- | --- |
| Product | Reference, Name, Price, Stock, TaxRate | Blank Id creates a new product. A UUID Id updates that existing product, including its absolute stock. Description is optional. |
| Customer | Reference, DocumentNumber, FirstName, LastName, Email, Phone, Address | UUID Id updates the identified profile. Without Id, document number identifies an existing profile or creates one. No account is automatically linked. |
| Sale | Reference, CustomerReference, ProductReference, Quantity, IdempotencyKey | Each row is an item; rows sharing Reference form one sale. Id must be empty. Prices and taxes come from stored products. |

References are unique within Product and Customer types and local to the workbook. Sale references group up to 100 distinct products and must share one customer and idempotency key. CustomerReference/ProductReference resolve either a local reference or an existing UUID. Product and Customer rows are normalized first even when sale rows appear earlier in the sheet.

Example values, shown as a table rather than a comma-delimited workbook:

| Type | Reference | Values |
| --- | --- | --- |
| Product | p1 | Name=Hammer, Price=100, Stock=5, TaxRate=0.19 |
| Customer | c1 | DocumentNumber=123456, FirstName=Jane, LastName=Doe, Email=jane@example.com, Phone=3001234567, Address=Main Street |
| Sale | s1 | CustomerReference=c1, ProductReference=p1, Quantity=2, IdempotencyKey=import-sale-001 |

All unspecified cells are blank. The result has `appliedRows` and `errors` with `row`, `code`, and `message`. Row numbers include the header, so the first data row is row 2. Any row or relationship/business error rolls back the complete workbook. A readable workbook with row errors returns 200 and `appliedRows: 0`; an invalid file returns 400; a size limit returns 413.

An identical successful workbook is replayed using its SHA-256 fingerprint and applies no further mutations. Do not reuse an already posted sale key in a modified workbook; that import fails without changes. For a new sale, supply a new idempotency key. Product-only updates remain supported when using a new workbook. Exact-byte replay intentionally takes priority over resynchronizing stock; make a new workbook for a deliberate inventory update.

Limits: 10 MiB compressed input, 50 MiB validated expanded data, 5,000 ZIP members, 10,000 data rows, 18 columns. Expanded streams, member CRC and metadata are checked and the workbook archive is rebuilt from validated contents before EPPlus parsing.

Administrative exports use `GET /api/v1/data/export/{resource}?format=xlsx` or `format=pdf`, with resource `products`, `customers`, `sales`, `vehicles`, or `rentals`. They include active and inactive records. Up to 10,000 parent records are exported; larger datasets are rejected rather than silently truncated. The Excel `Export` sheet contains DTO fields. Sales export has a second `SaleItems` sheet linked through `saleId`. Export layouts are reports and are not the mixed-row import template.
