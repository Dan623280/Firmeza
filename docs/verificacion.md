# Verificación realizada

Fecha: 23 de octubre de 2026 (America/Bogota).

| Comprobación | Resultado |
| --- | --- |
| Compilación .NET de toda la solución, Release | Correcta; 0 errores y 0 advertencias |
| Publicación de API en Release | Correcta |
| API publicada en Production sobre una base nueva | Arranque, migraciones y flujo real correctos |
| xUnit de negocio, importación y pruebas HTTP | 23 aprobadas, 0 fallidas |
| Compilación Angular en producción | Correcta; paquete inicial aproximado de 367 kB sin comprimir |
| Pruebas Node del carrito | Aprobadas: totales/IVA, redondeo y validación de cantidades |
| Migración inicial contra PostgreSQL 18 temporal | Aplicada correctamente, incluyendo Identity y relaciones |
| Script `scripts/smoke.py` contra API y PostgreSQL reales | Aprobado |
| Chrome de escritorio y móvil (390 px) | Sin errores JavaScript ni desbordamiento horizontal |
| Compra en navegador | Registro, carrito, confirmación y consulta de comprobante correctos |
| `docker compose config --quiet` con Compose 2.40.3 | Configuración válida |

El script de integración verifica registro, restricción de administradores, rechazo de edad decimal, compra con líneas repetidas, precios del servidor, IVA, stock, comprobante PDF, acceso exclusivo del dueño, falta de stock, anulación única, exportaciones Excel/PDF, reimportación y alquileres con fechas solapadas.

Las pruebas de importación verifican una fila con producto, cliente y venta; el log de edad inválida; y que exportar/reimportar productos conserve descripción y desactivación.

## Límites de la comprobación

- No se ejecutaron `docker compose up --build` ni las imágenes: Docker Engine no está instalado en el entorno de trabajo. Se validaron la configuración de Compose y las aplicaciones fuera de contenedores. El arranque completo de Docker queda pendiente en una máquina con Docker.
- No se envió un correo externo: no se proporcionaron credenciales SMTP. Se comprobó que la compra permanezca confirmada y el comprobante pueda descargarse cuando el correo está pendiente.
- La base de PostgreSQL utilizada en las pruebas es temporal y sus datos no forman parte del proyecto.
- Las pruebas HTTP de xUnit usan un repositorio en memoria; la prueba de integración usa PostgreSQL real.

## Capturas de la ejecución

[Tienda de escritorio](https://github.com/danielalva0004/firmeza/blob/main/docs/capturas/tienda.png) · [Tienda móvil](https://github.com/danielalva0004/firmeza/blob/main/docs/capturas/movil.png)

## Separación de backend y frontend

La solución .NET se trasladó a `backend/` y Angular a `frontend/`. Se ajustaron las rutas documentadas y los contextos Docker para construir cada aplicación de forma independiente. Tras el cambio se verificaron la compilación Release del backend sin advertencias, la compilación de producción y las pruebas de Angular, y la configuración de Compose y sus fuentes COPY.

## Administración integrada en Angular

Se verificó la compilación de producción con los módulos administrativos cargados bajo demanda. Las pruebas Node pasan: 5 casos de sesiones, expiración y decisiones de acceso por rol, y 3 casos del carrito.

Se recorrió el panel en Chrome contra la API real y una base temporal PostgreSQL:

- Login de administrador, redirección a `/admin` y restauración tras recargar.
- Creación, edición y desactivación de productos, clientes y vehículos.
- Venta con múltiples líneas, consolidación de cantidades repetidas, comprobante PDF y anulación con reposición de stock.
- Creación y cancelación de reservas.
- Descarga Excel/PDF e importación del Excel exportado.
- Cliente redirigido fuera de `/admin`, sin peticiones administrativas desde la pantalla, y respuesta 403 al intentar consumir directamente la API administrativa.
- Administrador redirigido al panel si intenta abrir rutas exclusivas del cliente.
- Sesión vencida redirigida al login.
- Pantalla de 390 px sin desbordamiento horizontal; sin errores JavaScript.

[Panel Angular](https://github.com/danielalva0004/firmeza/blob/main/docs/capturas/administracion-angular.png) · [Administración móvil](https://github.com/danielalva0004/firmeza/blob/main/docs/capturas/administracion-angular-movil.png)

El panel forma parte del mismo frontend; Compose mantiene únicamente API, Angular, PostgreSQL y pruebas. El envío SMTP externo y la ejecución de los contenedores conservan las limitaciones indicadas anteriormente.

También se repitió el flujo del cliente en Chrome después de integrar la administración: catálogo, registro, carrito, confirmación, historial y vista móvil aprobados, sin errores JavaScript.


> Las cuatro capturas anteriores permanecen alojadas en el repositorio de origen `danielalva0004/firmeza` y se enlazan desde aquí.
