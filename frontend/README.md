# Frontend Firmeza

Aplicación Angular independiente. Sus componentes y servicios están en `src/app/`.

Desde esta carpeta:

```bash
npm ci
npm start
```

La tienda abre en `http://localhost:4200`. El proxy de desarrollo dirige `/api` hacia `http://localhost:8080`, donde debe ejecutarse el backend.

```bash
npm test
npm run build
```

Esta carpeta tiene su propio Dockerfile y configuración Nginx. Para arrancar toda la infraestructura, ejecuta `docker compose up --build` desde la raíz del repositorio. Consulta el [README principal](../README.md).

## Acceso por rol

Inicia sesión en `/ingresar`. El administrador se redirige a `/admin`, con dashboard, productos, clientes, ventas, vehículos, alquileres y archivos. El cliente accede a la tienda y sus compras. La API autoriza cada operación además de los guards y el menú de Angular. Las credenciales del administrador se configuran en el `.env` de la raíz.

El panel está implementado en `src/app/admin/` y sus componentes se cargan bajo demanda.
