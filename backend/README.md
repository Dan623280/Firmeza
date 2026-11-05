# Backend Firmeza

Solución .NET independiente. Las capas de Clean Architecture están dentro de `src/`:

- `Firmeza.Domain`: entidades y reglas del negocio.
- `Firmeza.Application`: DTOs, interfaces y servicios.
- `Firmeza.Infrastructure`: PostgreSQL, Identity, repositorios, correo y archivos.
- `Firmeza.API`: endpoints REST y middleware.

Las pruebas xUnit están en `tests/Firmeza.Tests`.

Desde esta carpeta:

```bash
dotnet restore Firmeza.sln
dotnet build Firmeza.sln
dotnet test Firmeza.sln
```

Consulta las variables de entorno y las instrucciones de ejecución en el [README principal](../README.md). El frontend Angular está en `../frontend` y se comunica con la API mediante HTTP.
