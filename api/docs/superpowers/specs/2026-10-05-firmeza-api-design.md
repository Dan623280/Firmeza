# Firmeza Hardware Store API Design

## Purpose and scope

Build the backend for Firmeza, a hardware store selling products and renting vehicles. Use Clean Architecture and English identifiers, source comments, API contracts, and database names. Retain the existing .NET 10 target. Requirements originate from H1–H5 and Tablas.docx; this deliverable is the API, not the Razor administration interface or client frontend.

The user's affirmative answer is interpreted as including vehicles and rentals in the first version. This interpretation requires confirmation during design review.

## Architecture

Use a modular monolith with five projects:

- `Firmeza.Domain`: business rules; no dependencies on framework persistence or authentication packages.
- `Firmeza.Application`: use cases, DTOs, validation, mapping profiles, and interfaces for persistence, current user, authentication, receipts, imports, exports, and email. References Domain.
- `Firmeza.Infrastructure`: EF Core with Npgsql, migrations, Identity, JWT token creation, SMTP, file storage, and document implementations. References Application and Domain.
- `Firmeza.Api`: controllers, dependency injection, authentication and authorization policies, Swagger, CORS configuration, and centralized HTTP exception handling. References Application and Infrastructure.
- `Firmeza.Tests`: meaningful unit and integration tests.

Organize Application by feature. Use focused repositories and an explicit transaction boundary rather than a public generic CRUD abstraction. Keep business rules inside entities and value objects.

## Domain

Use the requested folders: `Abstractions`, `Entities`, `ValueObjects`, `Enums`, `Errors`, `Exceptions`, and `Constants`. Each folder contains actual functionality rather than empty placeholders.

- `Abstractions`: entity and aggregate base types with UUID identity and UTC auditing timestamps.
- `Entities`: `Product`, `Customer`, `Sale`, `SaleItem`, `Vehicle`, and `Rental`.
- `ValueObjects`: monetary values with explicit currency, document number, and rental date range.
- `Enums`: sale status and rental status.
- `Errors`: stable business error codes.
- `Exceptions`: a domain exception conveying a business error.
- `Constants`: business limits and default currency.

`Product` has name, description, price, integer stock, active flag, and timestamps. Stock cannot be negative and prices must be positive. Fractional quantities are outside this version, matching the supplied integer columns.

`Customer` has document number, first and last names, email, phone, address, active flag, and registration timestamp. Document number is unique. Its optional authentication user identifier is a string referencing an Infrastructure Identity account; business customers can exist before portal registration. Account linking requires validated ownership, never an unverified matching document number.

`Sale` belongs to a customer and owns sale items. It has a unique server-generated number, UTC date, status, subtotal, tax total, grand total, and optional receipt storage key. Items preserve product name, product identifier, unit price, quantity, and tax rate at purchase time. Clients submit product identifiers and quantities; the server determines all prices and totals. Use decimal arithmetic and explicit two-decimal rounding.

`Vehicle` has name, type, brand, unique license plate, daily price, and active flag. Availability is determined from overlapping rentals rather than maintained as an independent boolean that could disagree with bookings.

`Rental` belongs to a customer and vehicle, stores start and end dates, daily price snapshot, total, and status. Date intervals are start-inclusive and end-exclusive, with a minimum of one day. Non-cancelled reservations block overlapping intervals until completed. Historical completed bookings remain recorded but do not block future dates. Database transaction locking must serialize competing bookings for the same vehicle.

## Business workflows

Sale creation validates an active customer, non-empty distinct product lines, positive quantities, active products, and sufficient stock. It calculates totals and reduces stock in one database transaction. Concurrent purchases cannot oversell. An idempotency key prevents repeated client submissions from creating duplicate purchases.

Sales cannot have their financial history overwritten or be hard deleted. Cancellation is an explicit transition that restores stock exactly once. Administrative CRUD requirements are implemented through create/read and supported status transitions for posted sales; product and customer deletion deactivates records and preserves references.

Rental creation validates the customer, vehicle, date range, and overlap under a transaction. Total equals chargeable days times the server's daily price. Cancellation and completion are explicit, validated transitions. Posted rental amounts and date snapshots cannot be arbitrarily overwritten.

Tax rates are configurable business inputs with a persisted rate snapshot. Currency defaults to COP as an explicit proposal for this store; no assumption is made that all products share one legally applicable tax rate.

## Authentication and API boundaries

Use ASP.NET Core Identity in Infrastructure with JWT bearer authentication. Define English role identifiers `Administrator` and `Customer`. Public registration always creates a Customer; callers cannot select a privileged role. Initial administrator provisioning uses environment configuration and never checked-in credentials.

Public access: registration, login, and active product catalogue with search and pagination. Administrators manage catalogue, customers, vehicles, reporting, imports, and all business records. Customers can create sales and rentals for their own linked profile and access only their own history and receipts. Customer identifiers are derived from authenticated identity for customer requests.

Expose versioned REST controllers under `/api/v1` for authentication, products, customers, sales, vehicles, rentals, dashboard, and import/export operations. Use request and response DTOs with AutoMapper. Resource creation returns 201 with a location; lists use bounded pagination. Return Problem Details consistently: 400 invalid input, 401 unauthenticated, 403 forbidden, 404 absent or inaccessible owned resource, and 409 duplicate/state/stock/booking conflict. Never return stack traces or secrets.

Swagger documents bearer authentication. CORS uses configured frontend origins. Connection strings, signing keys, SMTP credentials, and bootstrap passwords come from environment configuration or user secrets.

## PostgreSQL and migrations

Create all schema through EF Core migrations, including Identity tables, relationships, decimal precision, unique indexes, and constraints. Use restricted deletes for historical business relationships. Provide a controlled migration command and a dedicated migration stage for Docker, avoiding concurrent application startup migrations.

Existing Razor database compatibility cannot be asserted: no Razor schema or migrations were supplied. Sharing the database later requires inspecting that project's actual Identity configuration and migration history first. This API design uses a new schema until that compatibility work is explicitly defined.

## Receipts, SMTP, and data exchange

Generate PDF sale receipts through an Application interface implemented with a suitable PDF library. Store receipts outside public static files and serve them through authorized endpoints. Receipts contain the sale number, customer, item snapshots, taxes, totals, and date; they are purchase receipts, not an asserted fiscal invoicing integration.

Persist email delivery work with the committed sale in an outbox. A worker generates the receipt and sends it through configurable SMTP, with bounded retries. Purchase success does not depend on SMTP availability. Expose receipt/delivery status so the frontend does not claim email was delivered prematurely. Gmail and enterprise SMTP use the same abstraction.

Excel import uses EPPlus as requested, subject to verifying an appropriate license before implementation. Use explicit documented column layouts for mixed product/customer/sale rows. Normalize in memory, validate required values, and return a row-level error report. Do not guess arbitrary workbook semantics. Set file and row limits. Importing sales must use the sale workflow and its stock/idempotency rules rather than directly inserting historical financial records.

Provide administrative Excel/PDF exports with bounded dataset sizes. Confirm external library licensing and package compatibility against official documentation during implementation.

## Verification and delivery

Test invalid monetary values, stock reduction, sale cancellation, financial snapshots, customer ownership, and rental intervals. Integration tests using PostgreSQL cover migrations, simultaneous stock purchases, competing reservations, authentication policies, and idempotent requests. Use test doubles for SMTP unit tests; real SMTP delivery requires supplied credentials and a controlled recipient.

Provide README setup instructions, environment examples without secrets, Mermaid entity and dependency diagrams, an API Dockerfile, a test Dockerfile, and Docker Compose for tests, PostgreSQL, migrations, and the API. Application services start only after tests succeed. Razor and frontend containers are outside this repository's current delivery scope.

## Implementation sequence

1. Establish the projects and implement/test the complete core domain.
2. Add Application use cases, DTOs, and persistence interfaces.
3. Implement PostgreSQL persistence, migrations, Identity, and JWT.
4. Expose and test API endpoints and authorization.
5. Implement receipts, SMTP outbox, and defined imports/exports.
6. Finish deployment configuration, documentation, and end-to-end verification.

The first implementation milestone is the domain and project boundaries, while retaining the complete API as the overall objective. A detailed execution plan follows approval of this written design.
