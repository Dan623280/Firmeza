# Firmeza Hardware Store API

A .NET 10 REST API for product sales and vehicle rentals, implemented with Clean Architecture and English code/API contracts.

## Projects

| Project | Responsibility | Dependencies |
| --- | --- | --- |
| `Firmeza.Domain` | Entities, value objects, business rules and errors | None |
| `Firmeza.Application` | Feature services, interfaces, DTOs and mapping profiles | Domain |
| `Firmeza.Infrastructure` | PostgreSQL, Identity/JWT, files, SMTP, Excel and PDF | Application, Domain |
| `FirmezaApi` | REST controllers, middleware, authorization and Swagger | Application, Infrastructure |
| `Firmeza.Tests` | xUnit business and PostgreSQL/HTTP integration tests | Tested projects |

Domain contains `Abstractions`, `Entities`, `ValueObjects`, `Enums`, `Errors`, `Exceptions`, and `Constants`. There are no Entity Framework or Identity dependencies in Domain. See [architecture and diagrams](docs/architecture.md).

## Run with Docker

Requires .NET-compatible Docker images and a working Docker Engine/Desktop with Compose.

```bash
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD, JWT_SIGNING_KEY, and bootstrap administrator credentials.
docker compose up --build
```

Generate the signing key with `openssl rand -base64 48`. Use a strong administrator password of at least 12 characters with uppercase, lowercase, numbers, and punctuation. No administrator password or signing key is included in the repository.

The sequence is `test-db → tests → db → migrations → api`. Tests use a separate disposable database. If tests fail, the application database, migrations and API do not start. API is available at `http://localhost:8080`; Swagger at `/swagger`. PostgreSQL and receipts use persistent volumes. Frontend and Razor projects are not included in this API's Compose file.

`docker compose down` stops services and preserves data. Rebuilding after code changes reruns tests. Do not remove volumes unless you intend to delete the stored data.

## Run locally

Requires a .NET 10 SDK and a PostgreSQL 16+ database created for Firmeza. Configure environment variables or development user secrets:

```bash
dotnet restore FirmezaApi.slnx
dotnet tool restore

dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Host=localhost;Port=5432;Database=firmeza;Username=YOUR_USER;Password=YOUR_PASSWORD' --project FirmezaApi
dotnet user-secrets set 'Jwt:SigningKey' 'YOUR_RANDOM_KEY_OF_AT_LEAST_32_BYTES' --project FirmezaApi
dotnet user-secrets set 'BootstrapAdmin:Email' 'YOUR_ADMIN_EMAIL' --project FirmezaApi
dotnet user-secrets set 'BootstrapAdmin:Password' 'YOUR_STRONG_ADMIN_PASSWORD' --project FirmezaApi

# Apply migrations and initialize Administrator/Customer roles and optional administrator.
ASPNETCORE_ENVIRONMENT=Development dotnet run --project FirmezaApi -- --migrate

dotnet run --project FirmezaApi --urls http://localhost:8080
```

Production uses the equivalent environment variables, such as `ConnectionStrings__DefaultConnection` and `Jwt__SigningKey`. An `.env` file is read by Docker Compose; `dotnet run` does not automatically load it.

All tables are created through EF Core migrations. API startup does not change the schema. Use `--migrate` as a separate deployment stage. To add a migration, run:

```bash
dotnet ef migrations add YourMigrationName --project Firmeza.Infrastructure --startup-project FirmezaApi
```

No existing Razor database schema was supplied. Do not apply these migrations to an existing administrative database until its Identity configuration and migration history have been checked for compatibility.

## Authentication

`POST /api/v1/auth/register` registers an ordinary `Customer`. It does not accept a privileged role. Registration creates the account and commercial profile in one transaction. An existing commercial document cannot be automatically linked without verification.

```json
{
  "documentNumber": "123456789",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane@example.com",
  "phone": "3001234567",
  "address": "Main Street 123",
  "password": "USE_YOUR_OWN_STRONG_PASSWORD"
}
```

`POST /api/v1/auth/login` accepts `email` and `password`. Both endpoints return `accessToken`, `expiresAt`, `roles`, and `customerId`. Send `Authorization: Bearer <accessToken>` on authenticated requests. JWT expiration defaults to 30 minutes. Signature, issuer, audience, lifetime, current account status, security stamp, current roles, and active customer profile are checked. There is no refresh-token endpoint in this version; authenticate again after expiration. Repeated failed login attempts lock the account temporarily.

In Swagger, select **Authorize** and enter the JWT token. Use the local administrator configured during `--migrate` to manage inventory and customers.

## Endpoints

All business endpoints use `/api/v1`. Response IDs are UUIDs. Lists accept `page`, `pageSize`, and supported `search` filters; default size is 20 and maximum size is 100. Only administrators can request inactive catalogue entries.

| Resource | Methods | Access |
| --- | --- | --- |
| `/auth/register`, `/auth/login` | POST | Public, rate limited |
| `/products`, `/products/{id}` | GET | Public active catalogue; administrator can include inactive |
| `/products`, `/products/{id}` | POST, PUT, DELETE | Administrator |
| `/customers`, `/customers/{id}` | GET, POST, PUT, DELETE | Administrator |
| `/vehicles`, `/vehicles/{id}` | GET | Public active vehicles; reservation availability checked at booking |
| `/vehicles`, `/vehicles/{id}` | POST, PUT, DELETE | Administrator |
| `/sales` | GET, POST | Customer's own sales; administrator can list all |
| `/sales/{id}` | GET | Owner or administrator; includes delivery status |
| `/sales/for-customer/{customerId}` | POST | Administrator |
| `/sales/{id}/cancel` | POST | Administrator |
| `/sales/{id}/receipt` | GET | Owner or administrator |
| `/rentals` | GET, POST | Customer's own rentals; administrator can list all |
| `/rentals/{id}` | GET | Owner or administrator |
| `/rentals/for-customer/{customerId}` | POST | Administrator |
| `/rentals/{id}/cancel` | POST | Owner or administrator |
| `/rentals/{id}/complete` | POST | Administrator |
| `/dashboard` | GET | Administrator |
| `/data/import` | POST multipart XLSX | Administrator |
| `/data/export/{resource}?format=xlsx` | GET; xlsx or pdf | Administrator |

DELETE deactivates products, customers, and vehicles. Posted sales and rentals are historical records; change status using the explicit transition endpoints rather than overwriting or deleting financial history. Responses use DTOs mapped with AutoMapper. Errors use Problem Details: 400 validation, 401 authentication, 403 permissions, 404 missing/inaccessible record, 409 state/stock/booking conflicts, and 503 unavailable document configuration.

### Products and sales

Product requests contain `name`, `description`, `price`, `stock`, and `taxRate`. Rates are fractions, for example `0.19`; configure the actual applicable rate per product. This code does not assert any legally required tax rate. Prices are in COP with at most two decimal places. Stock and quantities are integers; fractional stock is outside this version.

Creating a sale requires an `Idempotency-Key` header (1–100 characters) and only product IDs and quantities:

```json
{
  "items": [
    { "productId": "PRODUCT_UUID", "quantity": 2 }
  ]
}
```

The server calculates prices and taxes and saves historical snapshots. Customer identity comes from the authenticated account. Stock reduction, sale creation, idempotency and receipt-delivery enqueue commit together. Concurrent requests cannot oversell. Repeating the same key and items for the same customer returns the same sale; changing the items under that key returns 409. Cancellation restores stock once.

### Rentals

Vehicle requests contain `name`, `type`, `brand`, `licensePlate`, and `dailyPrice`. Rental requests contain `vehicleId`, `startDate`, and `endDate` in `yyyy-MM-dd` format. Start is inclusive; end is exclusive. June 1–3 bills two days. June 3–5 is adjacent and does not overlap. Allowed duration is 1–366 days. The API derives the customer, locks the vehicle during reservation checks, stores the daily price snapshot and rejects overlapping reserved rentals. Cancelled/completed rentals no longer block reservations. Retry a rental creation only after checking whether the previous response succeeded; rentals do not have sale-style idempotency in this version.

## Receipts and email

Receipts are private files outside `wwwroot`. Set `Licenses__QuestPdf` according to [library eligibility](docs/dependencies.md), `Delivery__Enabled=true`, and the SMTP variables listed in `.env.example`. For Gmail use an app password for the configured account and authenticated TLS SMTP, typically port 587. Port 465 uses implicit TLS. Enterprise SMTP can use the same service through configuration.

The delivery worker generates a PDF and sends it as an attachment. It uses a persisted lease and at most five attempts with increasing delays. A failed SMTP request does not roll back a purchase. Delivery statuses are `Pending`, `Processing`, `Sent`, or `Failed`; `Sent` means the SMTP server accepted the message, not that the recipient read it. SMTP delivery is at least once and can duplicate a message if the worker crashes after server acceptance.

`GET /sales/{id}` includes delivery status. Receipt download returns 409 until a file exists. With delivery disabled, messages remain pending. Configure the license and SMTP, then enable delivery to process them. Exhausted messages remain Failed for investigation; no public retry-management endpoint is provided. A receipt records the sale state at generation time and is not a regulated electronic tax invoice.

## Excel and PDF exchange

See [the exact workbook format](docs/import-format.md). Imports normalize mixed Product, Customer, and Sale rows from a `Records` sheet. Invalid rows return an error report; the entire workbook is atomic. Identical workbook bytes are recognized as a replay and do not repeat mutations. A changed workbook cannot reuse a posted sale key.

EPPlus operations require `Licenses__EPPlus`; commercial operation needs the appropriate EPPlus license. The application does not silently assume noncommercial use. See [dependency licenses and configuration](docs/dependencies.md). Configure `AutoMapper__LicenseKey` according to its terms as well.

Exports accept `products`, `customers`, `sales`, `vehicles`, and `rentals`. Excel exports write user text as literal cell values. Sales exports include a separate `SaleItems` sheet linked by `saleId`, avoiding large JSON cells. Import limits: 10 MiB compressed, 50 MiB validated expanded content, 5,000 ZIP members, 10,000 data rows, and 18 columns. Export limits are documented with the implementation in [import-format.md](docs/import-format.md).

## Tests and verification

```bash
# Domain tests run without external services. Database tests explicitly skip if no connection is supplied.
dotnet test FirmezaApi.slnx --configuration Release

# Full suite: point to a dedicated PostgreSQL instance whose user can CREATE/DROP test databases.
export FIRMEZA_TEST_DATABASE='Host=localhost;Port=5432;Database=postgres;Username=TEST_USER;Password=TEST_PASSWORD'
dotnet test FirmezaApi.slnx --configuration Release
```

Integration tests create uniquely named `firmeza_test_*` databases and drop them after use. Use a dedicated test server. Coverage includes real migrations, HTTP authentication/ownership, stock and booking races, rollback, import replay, stale customer state, PDF generation, mocked SMTP failures, corrupt workbooks and exports. No production database or email account is used by automated tests.

`/health/live` checks process availability. `/health/ready` checks database connectivity. `FirmezaApi/FirmezaApi.http` contains request examples.

The local implementation was tested with PostgreSQL independently of Docker. Docker Compose configuration was validated, but container startup could not be executed because Docker Desktop failed to start its virtual machine. Actual SMTP delivery still requires configured credentials and a designated recipient. No website or frontend has been deployed.
