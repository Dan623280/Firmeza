# Trazabilidad de H1–H5

| Entrega | Implementación |
| --- | --- |
| H1: estructura y base | Solución Firmeza, cuatro capas API en backend/src, EF Core/Npgsql y migración inicial |
| H1: Identity y roles | AuthService, inicialización de roles y autorización JWT en la API |
| H1: dashboard y CRUD | Pantallas Angular de dashboard, productos, clientes y ventas conectadas a la API; validación de edad entera |
| H1: diseño y documentación | CSS adaptable, README, diagramas Mermaid |
| H2: carga Excel | ExchangeService: cabeceras prefijadas, múltiples hojas, normalización y errores por fila |
| H2: exportación y PDF | Reportes productos/clientes/ventas y ReceiptService; importación/exportación desde /admin/archivos y comprobantes desde Angular |
| H3: API y DTOs | Controllers, contratos en Application/DTOs y MappingProfile de AutoMapper |
| H3: JWT y Swagger | Identity, JWT con roles, Swagger Bearer |
| H3/H4: correo | IEmailService y SmtpEmailService; configurable por entorno, adjunto PDF, estado real de envío |
| H4: frontend Angular | Catálogo, carrito, registro/login, expiración JWT, compras propias y comprobantes |
| H4/Tablas: alquileres | Vehicle/Rental, gestión administrativa, reservas con control de fechas y solapamiento |
| H5: pruebas | Proyecto Firmeza.Tests con xUnit y pruebas HTTP; Node test para carrito, sesiones y acceso por rol |
| H5: Docker | Dockerfiles API/Client/Tests, Compose con pruebas obligatorias y healthchecks |

## Decisiones de alcance

- El alcance solicitado es una API con Clean Architecture y un frontend Angular separados. La administración se integra en Angular mediante rutas /admin protegidas por rol y endpoints autorizados de la API.
- .NET 10 satisface la versión mínima solicitada y coincide con el SDK disponible.
- Las ventas se anulan para preservar trazabilidad; no se borran ni se alteran sus importes históricos.
- La API y el carrito Angular admiten ventas con múltiples líneas.
- La tienda utiliza CSS propio adaptable, sin CDN ni recursos gráficos remotos.
- No hay cobro por tarjeta ni pasarela de pago: la compra registra la operación comercial.
- El importador requiere cabeceras conocidas; no infiere esquemas arbitrarios. Su procesamiento incremental y las precauciones para reimportar ventas se explican en el README.
- El reenvío de comprobantes es manual; no existe cola de correo automática.

## Organización del repositorio

`backend/` contiene la solución .NET, las capas en `src/` y las pruebas xUnit. `frontend/` contiene exclusivamente Angular, con su package.json, configuración y Dockerfile. Compose coordina ambos desde la raíz usando contextos de construcción separados.
