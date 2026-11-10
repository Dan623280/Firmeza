# Firmeza

Sistema de venta de materiales de construcción y alquiler de vehículos, desarrollado a partir de H1–H5 y las tablas entregadas. API REST con Clean Architecture, frontend Angular con tienda y administración, y PostgreSQL. .NET 10 / Angular 21 / Node 24 / PostgreSQL 18.

## Ejecutar con Docker

Requisitos: Docker Engine o Docker Desktop y Docker Compose v2 actualizado.

```bash
cp .env.example .env
# Editar .env: POSTGRES_PASSWORD, JWT_KEY y ADMIN_PASSWORD.
docker compose up --build
```

| Servicio | Dirección |
| --- | --- |
| Angular: tienda e inicio de sesión | http://localhost:4200 |
| Administración Angular (rol Administrador) | http://localhost:4200/admin |
| Swagger con autorización Bearer | http://localhost:8080/swagger |
| Estado API | http://localhost:8080/health |

La cuenta de administrador se crea con `ADMIN_EMAIL` y `ADMIN_PASSWORD` de `.env`. Ingresa en `/ingresar` con esas credenciales para acceder al panel `/admin`. Las cuentas con rol Cliente se redirigen al catálogo y no pueden abrir rutas administrativas. Los clientes se registran desde la tienda. No se permite registrar administradores mediante la API pública. Los productos y vehículos de demostración se crean únicamente si `SEED_DEMO_DATA=true` y el inventario está vacío.

El servicio `tests` compila la solución del backend y al arrancar ejecuta xUnit. La imagen del frontend ejecuta sus pruebas y compila Angular durante su construcción. PostgreSQL espera `service_completed_successfully` de `tests`; API espera las pruebas y PostgreSQL saludable; Angular espera la API saludable. Un fallo de pruebas impide iniciar los servicios dependientes. En un despliegue que ya estaba funcionando, Compose no detiene automáticamente contenedores anteriores: para validar desde cero, ejecutar primero `docker compose down` (conserva los datos).

La API aplica migraciones EF Core al arrancar y crea los roles. PostgreSQL y los comprobantes usan volúmenes persistentes.

```bash
docker compose logs -f tests api
docker compose down
# No usar `down -v` salvo que se quiera borrar TODOS los datos persistidos.
```

## Estructura

```text
backend/
  Firmeza.sln
  Directory.Build.props
  global.json
  src/
    Firmeza.Domain/
      Abstractions/ Constants/ Entities/ Enums/ Errors/ Exceptions/ ValueObjects/
    Firmeza.Application/
      DTOs/ Exceptions/ Interfaces/ Services/
    Firmeza.Infrastructure/
      Persistence/
        AppDbContext.cs
        Migrations/
        Repositories/
      External/
      Services/
    Firmeza.API/
      Controllers/
      Middleware/
  tests/Firmeza.Tests/
frontend/                  # Aplicación Angular independiente
  src/app/
    admin/                 # Dashboard y pantallas administrativas por rol
  package.json
  angular.json
  Dockerfile
  nginx.conf
docs/
scripts/
docker-compose.yml
```

Backend y frontend tienen sus propios archivos de configuración, dependencias y contextos de construcción Docker. Angular se comunica con la API mediante HTTP.

Referencias: `Domain ← Application ← Infrastructure ← API`. Application usa interfaces de persistencia, correo y comprobantes; Infrastructure las implementa. Domain no conoce EF Core, Identity ni HTTP. El nombre `Constants` corresponde a la carpeta de constantes solicitada; `Middleware` contiene el manejo centralizado de errores.

## Funcionalidades

- Identity, roles Administrador/Cliente, JWT de dos horas, bloqueo tras intentos fallidos y limitación de solicitudes de autenticación.
- Panel administrativo dentro de Angular (`/admin`): dashboard, CRUD de productos/clientes/vehículos, búsquedas, filtros de estado, ventas con múltiples productos, comprobantes y reenvío de correo, reservas, importación Excel y exportación Excel/PDF.
- Guards de Angular para administrador/cliente, menú según el rol y validación de permisos en la API en cada petición.
- Tienda Angular con catálogo, búsqueda, ordenamiento, registro/login, carrito, historial, PDF y reservas por fechas.
- Ventas con detalles, precios calculados en el servidor, IVA del 19%, stock transaccional y aislamiento serializable. Reintentar una operación que devuelve 409 requiere actualizar primero la disponibilidad. La compra registra una venta; no hay pasarela de pagos.
- Eliminar productos/clientes/vehículos los desactiva para preservar el historial. Eliminar una venta la anula y repone stock una sola vez. No se editan importes históricos: se anula y registra una nueva venta.
- Reservas sin solapamiento por vehículo; el día de devolución no se cobra. Las fechas se validan usando UTC.
- EPPlus para Excel, QuestPDF para PDF y MailKit para SMTP con STARTTLS.
- Comprobantes almacenados en `wwwroot/recibos`, descargables mediante endpoint autenticado. La carpeta no se publica como contenido estático.
- Errores de validación HTTP 400, autenticación 401, autorización 403, no encontrado 404 y conflictos 409 mediante ProblemDetails.

## Acceso a la administración

El frontend usa un único inicio de sesión en `/ingresar`. El administrador accede a `/admin`, `/admin/productos`, `/admin/clientes`, `/admin/ventas`, `/admin/vehiculos`, `/admin/alquileres` y `/admin/archivos`. El cliente conserva el catálogo, carrito, compras y reservas. No se requiere otro proyecto ni otro puerto para administrar.

Los registros se desactivan para conservar el historial y pueden reactivarse editando su estado. Anular una venta repone el stock; cancelar un alquiler libera esas fechas. Las acciones se confirman antes de ejecutarse. Las credenciales iniciales están en `.env`; cambiar `ADMIN_PASSWORD` no cambia la contraseña de una cuenta ya creada.

## SMTP real

Configura en `.env`:

```dotenv
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu-cuenta@gmail.com
SMTP_PASSWORD=tu-password-de-aplicacion
SMTP_FROM=tu-cuenta@gmail.com
```

Utiliza una contraseña de aplicación de Gmail para la cuenta configurada. Las credenciales no se guardan en el repositorio. `IEmailService` permite sustituir Gmail por otro proveedor SMTP.

La compra se confirma antes del envío. Si SMTP no está configurado o falla, `emailSent=false`; la interfaz informa que el envío está pendiente y permite descargar el PDF o reintentar con `POST /api/ventas/{id}/reenviar`. No hay un trabajador de reintentos automático: el reenvío es explícito. El envío real necesita credenciales válidas y conectividad SMTP.

## API y roles

| Endpoint | Acceso |
| --- | --- |
| `POST /api/auth/register`, `POST /api/auth/login` | Público |
| `GET /api/productos`, `GET /api/productos/{id}` | Público (solo activos para clientes) |
| `POST/PUT/DELETE /api/productos` | Administrador |
| `GET/POST/PUT/DELETE /api/clientes` | Administrador |
| `GET /api/vehiculos` | Público |
| `POST/PUT/DELETE /api/vehiculos` | Administrador |
| `GET/POST /api/ventas` | Cliente (propias) / Administrador |
| `GET /api/ventas/{id}` | Dueño / Administrador |
| `GET /api/ventas/{id}/comprobante` | Dueño / Administrador |
| `POST /api/ventas/{id}/reenviar` | Dueño / Administrador |
| `PUT /api/ventas/{id}/anular`, `DELETE /api/ventas/{id}` | Administrador |
| `GET/POST /api/alquileres` | Cliente (propios) / Administrador |
| `DELETE /api/alquileres/{id}` | Dueño / Administrador |
| `GET /api/admin/dashboard` | Administrador |
| `POST /api/admin/importar` | Administrador, multipart `file` |
| `GET /api/admin/exportar/{recurso}/{formato}` | Administrador |

Ejemplo de registro:

```json
{
  "email": "cliente@example.com",
  "password": "MiClaveSegura2026!",
  "customer": {
    "document": "123456789", "name": "Ana", "lastName": "Pérez",
    "email": "cliente@example.com", "phone": "3001234567",
    "address": "Calle 10 # 20-30", "age": 25
  }
}
```

Login: `{"email":"cliente@example.com","password":"MiClaveSegura2026!"}`. Copia `token` de la respuesta en **Authorize** de Swagger. Las siguientes peticiones usan `Authorization: Bearer <token>`.

Compra:

```json
{"items":[{"productId":"UUID_DEL_PRODUCTO","quantity":2}]}
```

Un administrador añade `customerId`. Para clientes, la API determina el cliente desde el usuario autenticado y descarta cualquier `customerId` enviado. Los totales y precios enviados por un cliente no se utilizan.

Angular conserva el JWT en `sessionStorage`, incluye la cabecera Bearer y cierra la sesión al expirar o recibir 401. La publicación externa debe realizarse detrás de un proxy HTTPS; los puertos HTTP de Compose sirven para ejecución local. No publicar las credenciales de `.env`.

## Importación Excel

Máximo 10 MB y 5000 filas por hoja. Primera fila: cabeceras. Se ignoran espacios, tildes y diferencias entre mayúsculas/minúsculas. Se procesan todas las hojas.

| Productos | Clientes | Venta opcional |
| --- | --- | --- |
| ProductoNombre | ClienteDocumento | VentaCantidad |
| ProductoDescripcion | ClienteNombre | |
| ProductoPrecio | ClienteApellido | |
| ProductoStock | ClienteCorreo | |
| | ClienteTelefono | |
| | ClienteDireccion | |
| | ClienteEdad | |

Puedes mezclar productos y clientes en una fila, o usar filas y hojas separadas. Documento identifica clientes y nombre identifica productos existentes. Se crean o actualizan los registros. Para crear una venta, la fila debe incluir producto, cliente y cantidad. Precio y stock deben ser números reales de Excel o texto con punto decimal, sin separadores de miles; stock, edad y cantidad deben ser enteros. Si el Excel procede de otra fuente, ajusta las cabeceras a este contrato.

La importación es incremental: los registros válidos anteriores se conservan si después aparece un error. El resultado informa contadores y errores por hoja/fila; una fila mixta puede haber guardado su cliente antes de detectar un error del producto. No es una operación atómica de todo el archivo. Importar ventas nuevamente crea nuevas ventas: evita reimportar archivos ya procesados. Los productos y clientes se pueden exportar en un formato reutilizable para su importación. Las ventas se exportan como reporte, no como plantilla de reimportación.

Descarga Excel/PDF de productos, clientes o ventas en `/admin/archivos`. También puedes consumir los endpoints administrativos de la API. Los recibos de venta incluyen cliente, fecha, número, detalle, subtotal, IVA y total.

## Desarrollo local

Requisitos: SDK .NET 10, Node 24, npm y PostgreSQL 18.

```bash
dotnet restore backend/Firmeza.sln
cd backend && dotnet tool restore && cd ..
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=firmeza;Username=firmeza;Password=TU_CLAVE'
export Jwt__Key='UNA_CLAVE_ALEATORIA_DE_AL_MENOS_32_CARACTERES'
export Seed__AdminEmail='admin@firmeza.local'
export Seed__AdminPassword='TU_CLAVE_ADMIN_CON_10_CARACTERES_O_MAS!'
export Seed__DemoData=true
dotnet run --project backend/src/Firmeza.API --urls http://localhost:8080
```

En otra terminal, para Angular:

```bash
cd frontend
npm ci
npm start
```

El proxy de Angular dirige `/api` a `http://localhost:8080`; en Docker lo hace Nginx. No se incluye `.env` en las imágenes. `dotnet run` no carga `.env` automáticamente: usa las variables de entorno indicadas.

Crear migraciones adicionales:

```bash
cd backend
dotnet ef migrations add NombreCambio --project src/Firmeza.Infrastructure --startup-project src/Firmeza.API --output-dir Persistence/Migrations
cd ..
```

La migración inicial está incluida; nunca se usa `EnsureCreated` ni scripts SQL manuales para construir el esquema.

## Pruebas

```bash
dotnet test backend/Firmeza.sln
npm --prefix frontend test
npm --prefix frontend run build
```

xUnit comprueba reglas de inventario, IVA, consolidación de líneas repetidas, propiedad de ventas, anulación y disponibilidad de vehículos. Las pruebas HTTP verifican JWT, restricciones de roles y validación de controladores con un repositorio en memoria. Las pruebas Node cubren también sesiones, expiración y decisiones de acceso según el rol. Estas pruebas no requieren PostgreSQL y no sustituyen una prueba de integración contra la base real. `scripts/smoke.py` recorre un flujo real contra una API en ejecución y crea datos de prueba.

Consulta [diagramas](docs/arquitectura.md), [trazabilidad de requisitos](docs/requisitos.md) y [verificación](docs/verificacion.md).

## Licencias de librerías

EPPlus 8 está configurado por defecto para este proyecto académico no comercial; para uso comercial configura `EPPLUS_LICENSE` con tu licencia. Revisa también los términos de AutoMapper 16 (`AUTOMAPPER_LICENSE`) y QuestPDF (actualmente configurado como Community) antes de un despliegue comercial. Fuentes: [EPPlus](https://epplussoftware.com/docs/8.1/api/index.html), [AutoMapper](https://docs.automapper.io/en/stable/License-configuration.html), [QuestPDF](https://www.questpdf.com/license/). Compatibilidad del frontend: [Angular](https://angular.dev/reference/versions).
