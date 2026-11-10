# Arquitectura

```mermaid
flowchart LR
 Browser[Angular: tienda Cliente y panel Administrador] -->|HTTP /api + JWT| API[API REST]
 API --> Application[Application: DTOs y servicios]
 Application --> Domain[Domain: entidades y reglas]
 Infra[Infrastructure: EF, Identity, SMTP, Excel, PDF] -. implementa interfaces .-> Application
 API --> Infra
 Infra --> PostgreSQL[(PostgreSQL)]
 Infra --> SMTP[Servidor SMTP]
 Infra --> Receipts[Volumen comprobantes]
```

## Modelo entidad-relación

```mermaid
erDiagram
 AspNetUsers ||--o| Clientes : autentica
 AspNetUsers }o--o{ AspNetRoles : pertenece
 Clientes ||--o{ Ventas : compra
 Ventas ||--|{ DetalleVentas : contiene
 Productos ||--o{ DetalleVentas : referencia
 Clientes ||--o{ Alquileres : reserva
 Vehiculos ||--o{ Alquileres : corresponde
 Clientes {
  uuid Id PK
  text UserId FK
  string Document UK
  string Name
  string LastName
  string Email UK
  string Phone
  string Address
  int Age
  bool Active
 }
 Productos {
  uuid Id PK
  string Name
  decimal Price
  int Stock
  bool Active
 }
 Ventas {
  uuid Id PK
  uuid CustomerId FK
  string Number UK
  datetime Date
  decimal Subtotal
  decimal Tax
  decimal Total
  string Status
  string ReceiptPath
  bool EmailSent
 }
 DetalleVentas {
  uuid Id PK
  uuid SaleId FK
  uuid ProductId FK
  string ProductName
  int Quantity
  decimal UnitPrice
  decimal Subtotal
 }
 Vehiculos {
  uuid Id PK
  string Plate UK
  string Name
  string Type
  string Brand
  decimal PricePerDay
  bool Available
  bool Active
 }
 Alquileres {
  uuid Id PK
  uuid CustomerId FK
  uuid VehicleId FK
  date StartDate
  date EndDate
  decimal PricePerDay
  decimal Total
  string Status
 }
```

Los nombres de tablas son los del documento; los nombres de propiedades del código están en inglés. Los importes históricos y nombres de producto quedan conservados en DetalleVentas. Identity añade sus tablas de claims, tokens, logins y roles mediante la misma migración EF.

## Diagrama de clases

```mermaid
classDiagram
 Entity <|-- Product
 Entity <|-- Customer
 Entity <|-- Sale
 Entity <|-- SaleDetail
 Entity <|-- Vehicle
 Entity <|-- Rental
 Sale *-- SaleDetail
 Sale --> Customer
 SaleDetail --> Product
 Rental --> Vehicle
 Rental --> Customer
 CatalogService --> IUnitOfWork
 SalesService --> IUnitOfWork
 SalesService --> IReceiptService
 SalesService --> IEmailService
 UnitOfWork ..|> IUnitOfWork
 ReceiptService ..|> IReceiptService
 SmtpEmailService ..|> IEmailService
 AuthService ..|> IAuthService
 ExchangeService ..|> IExchangeService
 UnitOfWork --> AppDbContext
 class Product {
  +decimal Price
  +int Stock
  +RemoveStock(quantity)
 }
 class Sale {
  +CalculateTotals()
 }
 class SalesService {
  +Create()
  +Cancel()
  +Rent()
  +Deliver()
 }
```

## Arranque

```mermaid
flowchart TD
 A[Construir imágenes + compilar Angular] --> T[tests: xUnit]
 T -->|salida 0| DB[PostgreSQL saludable]
 DB --> API[API: migraciones + roles + seed]
 API -->|health OK| SPA[Angular + Nginx]
 T -->|salida distinta de 0| STOP[Dependientes bloqueados]
```
