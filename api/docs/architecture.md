# Architecture and domain model

```mermaid
flowchart LR
    API[Firmeza.Api] --> APP[Firmeza.Application]
    API --> INF[Firmeza.Infrastructure]
    INF --> APP
    INF --> DOM[Firmeza.Domain]
    APP --> DOM
    INF --> PG[(PostgreSQL)]
    INF --> SMTP[SMTP]
    INF --> PDF[Private receipt storage]
```

Business rules live in domain entities. Application services coordinate transactions through interfaces. Infrastructure owns EF Core/Identity, locks, document libraries and external delivery. Controllers expose DTOs and authorization rules. There is no web/persistence dependency in Domain.

```mermaid
erDiagram
    ApplicationUser o|--o| Customer : authenticates
    Customer ||--o{ Sale : purchases
    Sale ||--|{ SaleItem : contains
    Product ||--o{ SaleItem : references
    Customer ||--o{ Rental : reserves
    Vehicle ||--o{ Rental : rented
    Sale ||--o| OutboxMessage : schedules
    Sale ||--o| IdempotencyRecord : deduplicates
    Customer ||--o{ IdempotencyRecord : scopes
    Product {
        uuid Id PK
        string Name
        decimal Price
        decimal TaxRate
        int Stock
        bool IsActive
    }
    Customer {
        uuid Id PK
        string UserId FK
        string DocumentNumber UK
        string Email
        bool IsActive
    }
    Sale {
        uuid Id PK
        uuid CustomerId FK
        string Number UK
        decimal Subtotal
        decimal TaxTotal
        decimal Total
        string Status
    }
    SaleItem {
        uuid Id PK
        uuid SaleId FK
        uuid ProductId FK
        int Quantity
        decimal UnitPrice
        decimal TaxRate
        decimal Subtotal
        decimal TaxTotal
    }
    Vehicle {
        uuid Id PK
        string LicensePlate UK
        decimal DailyPrice
        bool IsActive
    }
    Rental {
        uuid Id PK
        uuid CustomerId FK
        uuid VehicleId FK
        date StartDate
        date EndDate
        decimal DailyPrice
        decimal Total
        string Status
    }
    ImportBatch {
        string Hash PK
        int AppliedRows
        timestamp CreatedAt
    }
```

Standard Identity users, roles, claims, logins and token tables are managed by Identity migrations. Domain Customer only stores an optional user identifier, not an Identity entity or credentials.

```mermaid
classDiagram
    Entity <|-- AggregateRoot
    Entity <|-- SaleItem
    AggregateRoot <|-- Product
    AggregateRoot <|-- Customer
    AggregateRoot <|-- Sale
    AggregateRoot <|-- Vehicle
    AggregateRoot <|-- Rental
    Sale "1" *-- "1..100" SaleItem
    Product ..> Money
    Vehicle ..> Money
    Rental ..> RentalPeriod
    Customer ..> DocumentNumber
    class Product {
        +ReduceStock(quantity)
        +RestoreStock(quantity)
        +Deactivate()
    }
    class Sale {
        +Create(customerId,number,items)
        +Cancel()
    }
    class Rental {
        +Create(customerId,vehicleId,period,dailyPrice)
        +Cancel()
        +Complete()
    }
```

Sale creation acquires a customer-scoped idempotency advisory lock, locks/revalidates the customer, and locks products in UUID order. PostgreSQL row locks serialize competing stock writes. Stored item prices/tax rates do not change when catalogue prices change. Cancellation locks the sale and restores inventory only after a valid transition.

Rental creation locks/revalidates the customer and locks the vehicle before checking reserved date overlaps. Dates are calendar dates with an exclusive end; auditing instants are UTC. Availability is derived from reservations. Explicit row refresh prevents EF's tracking cache from reusing stale unchanged data after a concurrent update.

Imports use a workbook fingerprint lock plus a persisted ImportBatch. Normalization happens before writes, then the whole batch is transactional. Existing sale keys in changed workbooks are rejected before inventory changes. Files are never treated as arbitrary executable spreadsheet logic.

Receipt generation and SMTP delivery are processed from a persisted outbox. Claims use `FOR UPDATE SKIP LOCKED` and an expiring lease; completion updates only the owning lease. At most five attempts are allowed. Receipt files are served only after sale ownership checks.
