-- =============================================
-- BASE DE DATOS FIRMEZA
-- PostgreSQL
-- =============================================

-- Permite generar UUID automáticamente
CREATE EXTENSION IF NOT EXISTS pgcrypto;


-- =============================================
-- 1. CLIENTES
-- =============================================

CREATE TABLE clientes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- El documento indica que es FK,
    -- pero todavía no tenemos la tabla Usuarios.
    usuario_id TEXT,

    documento VARCHAR UNIQUE NOT NULL,
    nombre VARCHAR NOT NULL,
    apellido VARCHAR NOT NULL,
    telefono VARCHAR,
    direccion VARCHAR,

    fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    activo BOOLEAN DEFAULT TRUE
);


-- =============================================
-- 2. PRODUCTOS
-- =============================================

CREATE TABLE productos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    nombre VARCHAR(150) NOT NULL,
    descripcion VARCHAR(500),

    precio DECIMAL(12,2) NOT NULL,
    stock INTEGER NOT NULL DEFAULT 0,

    activo BOOLEAN DEFAULT TRUE,

    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fecha_actualizacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


-- =============================================
-- 3. VENTAS
-- =============================================

CREATE TABLE ventas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    cliente_id UUID NOT NULL,

    fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    subtotal DECIMAL(12,2) NOT NULL,
    iva DECIMAL(12,2) NOT NULL,
    total DECIMAL(12,2) NOT NULL,

    estado VARCHAR NOT NULL,

    numero_venta VARCHAR UNIQUE NOT NULL,

    ruta_comprobante VARCHAR,

    CONSTRAINT fk_ventas_cliente
        FOREIGN KEY (cliente_id)
        REFERENCES clientes(id)
);


-- =============================================
-- 4. DETALLE VENTAS
-- =============================================

CREATE TABLE detalle_ventas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    venta_id UUID NOT NULL,
    producto_id UUID NOT NULL,

    cantidad INTEGER NOT NULL,

    precio_unitario DECIMAL(12,2) NOT NULL,

    subtotal DECIMAL(12,2) NOT NULL,

    CONSTRAINT fk_detalle_venta
        FOREIGN KEY (venta_id)
        REFERENCES ventas(id),

    CONSTRAINT fk_detalle_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos(id)
);


-- =============================================
-- 5. VEHICULOS
-- =============================================
-- El PDF contiene estos campos, pero no indica
-- sus tipos. Estos tipos son una propuesta.

CREATE TABLE vehiculos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    nombre VARCHAR(150) NOT NULL,
    tipo VARCHAR(100) NOT NULL,
    marca VARCHAR(100),
    placa VARCHAR(20) UNIQUE NOT NULL,

    precio_dia DECIMAL(12,2) NOT NULL,

    disponible BOOLEAN DEFAULT TRUE,
    activo BOOLEAN DEFAULT TRUE
);


-- =============================================
-- 6. ALQUILERES
-- =============================================
-- El PDF tampoco especifica los tipos de esta
-- tabla. Estos tipos son una propuesta.

CREATE TABLE alquileres (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    cliente_id UUID NOT NULL,
    vehiculo_id UUID NOT NULL,

    fecha_inicio TIMESTAMP NOT NULL,
    fecha_fin TIMESTAMP NOT NULL,

    precio_dia DECIMAL(12,2) NOT NULL,
    total DECIMAL(12,2) NOT NULL,

    estado VARCHAR NOT NULL,

    CONSTRAINT fk_alquiler_cliente
        FOREIGN KEY (cliente_id)
        REFERENCES clientes(id),

    CONSTRAINT fk_alquiler_vehiculo
        FOREIGN KEY (vehiculo_id)
        REFERENCES vehiculos(id)
);
