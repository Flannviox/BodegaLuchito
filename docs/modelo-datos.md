# Modelo de datos - Bodega Luchito

## 1. Propósito

Este documento define el modelo de datos base que utilizará el sistema Bodega Luchito.

Su objetivo es servir como referencia común para el equipo de desarrollo y evitar que cada módulo defina entidades, tipos de datos o relaciones diferentes.

El modelo inicial contiene 10 entidades persistentes:

1. Usuario
2. Producto
3. Proveedor
4. Abastecimiento
5. DetalleAbastecimiento
6. MovimientoInventario
7. Venta
8. DetalleVenta
9. SesionCaja
10. MovimientoCaja

Los módulos Tickets, Reportes y Business Intelligence no requieren tablas propias en esta versión.

---

# 2. Autenticacion

## 2.1 Usuario

Representa a una persona autorizada para utilizar el sistema.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| NombreCompleto | string | Sí | Nombre completo del usuario |
| NombreUsuario | string | Sí | Nombre utilizado para iniciar sesión |
| PasswordHash | string | Sí | Contraseña almacenada mediante hash |
| Rol | RolUsuario | Sí | Rol asignado al usuario |
| Activo | bool | Sí | Indica si puede utilizar el sistema |
| FechaCreacion | DateTime | Sí | Fecha de creación |
| FechaActualizacion | DateTime? | No | Fecha de última modificación |

### Restricciones

- `NombreUsuario` debe ser único.
- La contraseña nunca debe almacenarse en texto plano.
- Un usuario con información histórica no debe eliminarse físicamente.
- Para deshabilitar un usuario se utiliza `Activo = false`.

### RolUsuario

```csharp
public enum RolUsuario
{
    Administradora = 1,
    Vendedora = 2
}
```

---

# 3. Productos

## 3.1 Producto

Representa un producto que puede ser vendido por la bodega.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| Nombre | string | Sí | Nombre del producto |
| Categoria | string | Sí | Categoría comercial |
| CodigoBarras | string? | No | Código utilizado por el lector |
| PrecioVenta | decimal | Sí | Precio actual de venta |
| UnidadVenta | UnidadVenta | Sí | Unidad o peso |
| ControlaInventario | bool | Sí | Indica si se controla stock |
| StockActual | decimal | Sí | Stock disponible |
| StockMinimo | decimal | Sí | Nivel mínimo esperado |
| Activo | bool | Sí | Indica si el producto puede utilizarse |
| FechaCreacion | DateTime | Sí | Fecha de registro |
| FechaActualizacion | DateTime? | No | Fecha de última modificación |

### Restricciones

- `Nombre` es obligatorio.
- `Categoria` es obligatoria.
- `PrecioVenta` debe ser mayor que cero.
- `CodigoBarras` es opcional.
- Cuando exista, `CodigoBarras` debe ser único.
- `StockActual` no puede ser negativo.
- `StockMinimo` no puede ser negativo.
- Si `ControlaInventario = false`, entonces:

```text
StockActual = 0
StockMinimo = 0
```

- Un producto con información histórica no debe eliminarse físicamente.
- Para retirarlo de uso se utiliza `Activo = false`.

### UnidadVenta

```csharp
public enum UnidadVenta
{
    Unidad = 1,
    Peso = 2
}
```

### Productos por peso

Las cantidades de productos vendidos por peso se almacenarán utilizando `decimal`.

La unidad base para el sistema será el kilogramo.

Ejemplo:

```text
350 gramos = 0.350 kg
```

Si el precio es:

```text
S/ 8.00 por kg
```

el subtotal será:

```text
0.350 × 8.00 = S/ 2.80
```

---

# 4. Proveedores

## 4.1 Proveedor

Representa a una persona o empresa que suministra productos a la bodega.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| Nombre | string | Sí | Nombre o razón comercial |
| Ruc | string? | No | RUC del proveedor |
| Telefono | string? | No | Número de contacto |
| Direccion | string? | No | Dirección |
| Activo | bool | Sí | Indica si está disponible |
| FechaCreacion | DateTime | Sí | Fecha de registro |
| FechaActualizacion | DateTime? | No | Fecha de última modificación |

### Restricciones

- `Nombre` es obligatorio.
- `Ruc` es opcional.
- Cuando exista, el RUC debe contener exactamente 11 dígitos.
- Cuando exista, el RUC debe ser único.
- Un proveedor con abastecimientos históricos no debe eliminarse físicamente.
- Para deshabilitarlo se utiliza `Activo = false`.

---

# 5. Abastecimientos

## 5.1 Abastecimiento

Representa la recepción de productos provenientes de un proveedor.

En Bodega Luchito el proveedor se paga al momento de recibir la mercadería, por lo que un abastecimiento confirmado también origina un egreso de caja.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| ProveedorId | int | Sí | FK hacia Proveedor |
| UsuarioId | int | Sí | FK hacia Usuario que registra |
| SesionCajaId | int | Sí | FK hacia SesionCaja |
| FechaHora | DateTime | Sí | Fecha y hora del abastecimiento |
| MetodoPago | MetodoPago | Sí | Medio utilizado para pagar |
| Total | decimal | Sí | Importe total |

### Restricciones

- Debe existir un proveedor válido.
- Debe contener al menos un detalle.
- `Total` debe ser mayor que cero.
- Debe registrarse el usuario responsable.
- El abastecimiento confirmado genera un movimiento de caja.
- Los productos inventariables generan movimientos de entrada de inventario.

---

## 5.2 DetalleAbastecimiento

Representa cada producto incluido dentro de un abastecimiento.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| AbastecimientoId | int | Sí | FK hacia Abastecimiento |
| ProductoId | int | Sí | FK hacia Producto |
| Cantidad | decimal | Sí | Cantidad recibida |
| CostoUnitario | decimal | Sí | Costo de adquisición por unidad |
| Subtotal | decimal | Sí | Cantidad multiplicada por CostoUnitario |

### Restricciones

- `Cantidad` debe ser mayor que cero.
- `CostoUnitario` no puede ser negativo.
- `Subtotal` corresponde a:

```text
Cantidad × CostoUnitario
```

### Relación

```text
Abastecimiento 1 ───── N DetalleAbastecimiento

Producto        1 ───── N DetalleAbastecimiento
```

---

# 6. Inventario

## 6.1 MovimientoInventario

Representa cualquier entrada, salida, ajuste o reversión de stock.

`Producto.StockActual` mantiene el estado actual del inventario.

`MovimientoInventario` mantiene el historial que explica cómo se llegó a dicho stock.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| ProductoId | int | Sí | FK hacia Producto |
| UsuarioId | int | Sí | FK hacia Usuario responsable |
| Tipo | TipoMovimientoInventario | Sí | Tipo de movimiento |
| Cantidad | decimal | Sí | Cantidad involucrada |
| StockAnterior | decimal | Sí | Stock antes del movimiento |
| StockPosterior | decimal | Sí | Stock después del movimiento |
| FechaHora | DateTime | Sí | Fecha y hora |
| Motivo | string? | No | Motivo, principalmente para ajustes |
| VentaId | int? | No | FK hacia Venta cuando corresponda |
| AbastecimientoId | int? | No | FK hacia Abastecimiento cuando corresponda |

### TipoMovimientoInventario

```csharp
public enum TipoMovimientoInventario
{
    EntradaAbastecimiento = 1,
    SalidaVenta = 2,
    AjusteEntrada = 3,
    AjusteSalida = 4,
    ReversionVenta = 5
}
```

### Ejemplo

```text
Producto: Coca Cola 500 ml

StockAnterior  = 20
Tipo           = SalidaVenta
Cantidad       = 2
StockPosterior = 18
VentaId        = 150
```

### Reglas

- `Cantidad` se almacena como un valor positivo.
- `Tipo` determina si representa entrada, salida, ajuste o reversión.
- Solo se generan movimientos para productos con:

```text
ControlaInventario = true
```

- Una modificación de stock debe actualizar `Producto.StockActual` y crear su `MovimientoInventario` dentro de la misma operación.

---

# 7. Ventas

## 7.1 Venta

Representa una venta confirmada en el punto de venta.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| UsuarioId | int | Sí | FK hacia Usuario que registra |
| SesionCajaId | int | Sí | FK hacia SesionCaja |
| FechaHora | DateTime | Sí | Fecha y hora |
| MetodoPago | MetodoPago | Sí | Efectivo, Yape o Plin |
| Total | decimal | Sí | Total de la venta |
| Estado | EstadoVenta | Sí | Confirmada o anulada |
| FechaAnulacion | DateTime? | No | Fecha de anulación |
| UsuarioAnulacionId | int? | No | FK hacia Usuario que anula |
| MotivoAnulacion | string? | No | Motivo de la anulación |

### EstadoVenta

```csharp
public enum EstadoVenta
{
    Confirmada = 1,
    Anulada = 2
}
```

### Reglas

- Una venta debe contener al menos un detalle.
- `Total` debe ser mayor que cero.
- Una venta confirmada genera un ingreso en caja.
- Los productos inventariables generan salidas de inventario.
- Una venta nunca debe eliminarse físicamente.
- Una anulación cambia su estado a `Anulada`.
- Una anulación debe conservar los detalles originales.
- La anulación debe registrar usuario, fecha y motivo.

---

## 7.2 DetalleVenta

Representa cada producto incluido en una venta.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| VentaId | int | Sí | FK hacia Venta |
| ProductoId | int | Sí | FK hacia Producto |
| NombreProducto | string | Sí | Nombre histórico del producto |
| UnidadVenta | UnidadVenta | Sí | Unidad utilizada al vender |
| Cantidad | decimal | Sí | Cantidad vendida |
| PrecioUnitario | decimal | Sí | Precio al momento de la venta |
| Subtotal | decimal | Sí | Cantidad multiplicada por PrecioUnitario |

### Información histórica

`NombreProducto`, `UnidadVenta` y `PrecioUnitario` se conservan dentro del detalle para mantener correctamente el historial.

Ejemplo:

```text
Septiembre:
Producto = Coca Cola
Precio = S/ 3.50

Octubre:
Producto = Coca Cola
Precio actual = S/ 4.00
```

La venta realizada en septiembre debe continuar mostrando:

```text
PrecioUnitario = S/ 3.50
```

### Relación

```text
Venta    1 ───── N DetalleVenta

Producto 1 ───── N DetalleVenta
```

---

# 8. Caja

## 8.1 SesionCaja

Representa una apertura y cierre de caja.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| UsuarioAperturaId | int | Sí | FK hacia Usuario |
| FechaApertura | DateTime | Sí | Inicio de la sesión |
| FondoInicial | decimal | Sí | Fondo inicial en efectivo |
| Estado | EstadoSesionCaja | Sí | Abierta o cerrada |
| UsuarioCierreId | int? | No | FK hacia Usuario que cierra |
| FechaCierre | DateTime? | No | Fecha y hora de cierre |
| EfectivoEsperado | decimal? | No | Importe calculado por el sistema |
| EfectivoReal | decimal? | No | Importe comprobado físicamente |
| DiferenciaEfectivo | decimal? | No | Real menos Esperado |
| YapeEsperado | decimal? | No | Yape esperado |
| YapeReal | decimal? | No | Yape verificado |
| DiferenciaYape | decimal? | No | Real menos Esperado |
| PlinEsperado | decimal? | No | Plin esperado |
| PlinReal | decimal? | No | Plin verificado |
| DiferenciaPlin | decimal? | No | Real menos Esperado |
| ObservacionCierre | string? | No | Comentario de cierre |

### EstadoSesionCaja

```csharp
public enum EstadoSesionCaja
{
    Abierta = 1,
    Cerrada = 2
}
```

### Reglas

- Solo puede existir una sesión de caja abierta a la vez.
- `FondoInicial` debe ser mayor o igual a cero.
- El fondo inicial no debe estar hardcodeado.
- El valor habitual de S/ 1000 se ingresa como valor de apertura y queda almacenado históricamente.
- Al cerrar se calculan valores esperados y diferencias.
- Una sesión cerrada no debe volver a recibir movimientos.

### Cálculo básico de efectivo

```text
EfectivoEsperado =
FondoInicial
+ Ingresos en efectivo
- Egresos en efectivo
- Reversiones correspondientes
```

### Cálculo básico de Yape y Plin

```text
Esperado =
Ingresos
- Egresos
- Reversiones correspondientes
```

### Diferencias

```text
Diferencia = ValorReal - ValorEsperado
```

---

## 8.2 MovimientoCaja

Representa un ingreso, egreso o reversión monetaria dentro de una sesión de caja.

| Campo | Tipo C# | Requerido | Descripción |
|---|---|---:|---|
| Id | int | Sí | Identificador único |
| SesionCajaId | int | Sí | FK hacia SesionCaja |
| UsuarioId | int | Sí | FK hacia Usuario |
| Tipo | TipoMovimientoCaja | Sí | Tipo de movimiento |
| MetodoPago | MetodoPago | Sí | Efectivo, Yape o Plin |
| Monto | decimal | Sí | Importe |
| FechaHora | DateTime | Sí | Fecha y hora |
| VentaId | int? | No | FK hacia Venta |
| AbastecimientoId | int? | No | FK hacia Abastecimiento |
| Descripcion | string? | No | Información adicional |

### TipoMovimientoCaja

```csharp
public enum TipoMovimientoCaja
{
    IngresoVenta = 1,
    EgresoAbastecimiento = 2,
    ReversionVenta = 3
}
```

### Reglas

- `Monto` debe ser mayor que cero.
- El signo del número no determina la operación.
- El campo `Tipo` determina si representa ingreso, egreso o reversión.
- Una venta confirmada genera `IngresoVenta`.
- Un abastecimiento confirmado genera `EgresoAbastecimiento`.
- Una venta anulada genera la reversión correspondiente.

---

# 9. Enums compartidos

## 9.1 MetodoPago

Este enum pertenece a un espacio compartido porque es utilizado por Ventas, Caja y Abastecimientos.

Ubicación sugerida:

```text
BodegaLuchito.Domain/
└── Shared/
    └── Enums/
        └── MetodoPago.cs
```

Definición:

```csharp
public enum MetodoPago
{
    Efectivo = 1,
    Yape = 2,
    Plin = 3
}
```

---

# 10. Resumen de relaciones

```text
Usuario
   │
   ├──── 1:N ──── Venta
   ├──── 1:N ──── Abastecimiento
   ├──── 1:N ──── MovimientoInventario
   ├──── 1:N ──── MovimientoCaja
   └──── 1:N ──── SesionCaja


Proveedor
   │
   └──── 1:N ──── Abastecimiento


Abastecimiento
   │
   ├──── 1:N ──── DetalleAbastecimiento
   ├──── 1:N ──── MovimientoInventario
   └──── 1:N ──── MovimientoCaja


Producto
   │
   ├──── 1:N ──── DetalleVenta
   ├──── 1:N ──── DetalleAbastecimiento
   └──── 1:N ──── MovimientoInventario


Venta
   │
   ├──── 1:N ──── DetalleVenta
   ├──── 1:N ──── MovimientoInventario
   └──── 1:N ──── MovimientoCaja


SesionCaja
   │
   ├──── 1:N ──── Venta
   ├──── 1:N ──── Abastecimiento
   └──── 1:N ──── MovimientoCaja
```

---

# 11. Flujos que afectan varias entidades

## 11.1 Confirmar venta

```text
Confirmar Venta
      │
      ├── Crear Venta
      │
      ├── Crear DetalleVenta
      │
      ├── Registrar MovimientoCaja
      │      Tipo = IngresoVenta
      │
      └── Por cada producto inventariable:
             │
             ├── disminuir Producto.StockActual
             │
             └── crear MovimientoInventario
                    Tipo = SalidaVenta

             ↓

      Confirmar transacción
             ↓
      Permitir generar Ticket
```

La creación de Venta, DetalleVenta, actualización de inventario y MovimientoCaja debe realizarse como una misma operación transaccional.

La impresión del ticket ocurre después de confirmar correctamente la venta.

Si la impresora falla:

```text
Venta               guardada
DetalleVenta         guardado
Inventario           actualizado
MovimientoCaja       guardado
Ticket físico        puede reintentarse
```

La venta no debe revertirse únicamente porque falle la impresora.

---

## 11.2 Confirmar abastecimiento

```text
Confirmar Abastecimiento
          │
          ├── Crear Abastecimiento
          │
          ├── Crear DetalleAbastecimiento
          │
          ├── Crear MovimientoCaja
          │      Tipo = EgresoAbastecimiento
          │
          └── Por cada producto inventariable:
                 │
                 ├── aumentar Producto.StockActual
                 │
                 └── crear MovimientoInventario
                        Tipo = EntradaAbastecimiento

                 ↓

          Confirmar transacción
```

---

## 11.3 Anular venta

```text
Anular Venta
     │
     ├── Estado = Anulada
     ├── FechaAnulacion
     ├── UsuarioAnulacionId
     ├── MotivoAnulacion
     │
     ├── Por cada producto inventariable:
     │      │
     │      ├── devolver stock
     │      └── crear MovimientoInventario
     │             Tipo = ReversionVenta
     │
     └── crear MovimientoCaja
            Tipo = ReversionVenta
```

La Venta y sus DetalleVenta originales permanecen almacenados.

---

# 12. Elementos sin tabla propia

## 12.1 Tickets

Los tickets internos se generan a partir de:

```text
Venta
+
DetalleVenta
+
datos necesarios para mostrar la operación
```

No se crea una tabla `Ticket` en esta versión.

La impresión pertenece funcionalmente al módulo Ventas.

---

## 12.2 Reportes

Los reportes se generan mediante consultas sobre las entidades existentes.

Ejemplos:

- ventas por fecha;
- ventas por producto;
- ventas por medio de pago;
- estado de inventario;
- movimientos de inventario;
- abastecimientos por proveedor;
- movimientos de caja;
- cierres de caja.

No existe una tabla `Reporte`.

---

## 12.3 Business Intelligence

Los indicadores se calculan utilizando información histórica registrada por los módulos operativos.

Ejemplos:

- productos más vendidos;
- categorías más vendidas;
- ventas por día;
- ventas por medio de pago;
- ingresos diarios;
- productos con baja rotación.

No existe una tabla `BI` ni una tabla `Dashboard`.

---

# 13. Consideraciones generales

Los productos pueden ser inventariables o no inventariables.

Los productos pueden venderse por unidad o por peso.

Las cantidades se almacenan mediante `decimal`.

Los importes monetarios se almacenan mediante `decimal`.

Los productos inventariables mantienen:

```text
Producto.StockActual
+
historial de MovimientoInventario
```

Los productos no inventariables pueden venderse normalmente, pero no generan movimientos de stock.

Las ventas confirmadas generan ingresos de caja.

Los abastecimientos confirmados generan egresos de caja.

Las ventas anuladas no se eliminan físicamente.

Los productos y proveedores con información histórica se desactivan en lugar de eliminarse.

Las operaciones importantes registran el usuario responsable cuando corresponde.

---

# 14. Funcionalidades consideradas para una etapa posterior

## 14.1 Lotes y vencimientos

La identificación de productos por lote y el control de fechas de vencimiento se consideran extensiones posteriores del modelo.

Por el momento no se implementarán:

- control detallado por lotes;
- estrategias FIFO o FEFO;
- alertas automáticas de vencimiento;
- selección automática de lotes durante una venta;
- descuento de stock por lote.

Si posteriormente los requerimientos lo exigen, podrá incorporarse una entidad:

```text
LoteInventario
```

relacionada con:

```text
Producto
Abastecimiento
DetalleAbastecimiento
```

con posibles campos como:

```text
Id
ProductoId
DetalleAbastecimientoId
FechaIngreso
FechaVencimiento
CantidadInicial
CantidadDisponible
Activo
```

Esta extensión no forma parte del Sprint 01 ni debe implementarse actualmente.

---

# 15. Ubicación sugerida en Domain

```text
BodegaLuchito.Domain/
│
├── Autenticacion/
│   ├── Entities/
│   │   └── Usuario.cs
│   └── Enums/
│       └── RolUsuario.cs
│
├── Productos/
│   ├── Entities/
│   │   └── Producto.cs
│   └── Enums/
│       └── UnidadVenta.cs
│
├── Proveedores/
│   └── Entities/
│       └── Proveedor.cs
│
├── Abastecimientos/
│   └── Entities/
│       ├── Abastecimiento.cs
│       └── DetalleAbastecimiento.cs
│
├── Inventario/
│   ├── Entities/
│   │   └── MovimientoInventario.cs
│   └── Enums/
│       └── TipoMovimientoInventario.cs
│
├── Ventas/
│   ├── Entities/
│   │   ├── Venta.cs
│   │   └── DetalleVenta.cs
│   ├── Enums/
│   │   └── EstadoVenta.cs
│   └── Tickets/
│
├── Caja/
│   ├── Entities/
│   │   ├── SesionCaja.cs
│   │   └── MovimientoCaja.cs
│   └── Enums/
│       ├── EstadoSesionCaja.cs
│       └── TipoMovimientoCaja.cs
│
└── Shared/
    └── Enums/
        └── MetodoPago.cs
```

---

# 16. Modelo v1

El modelo de datos v1 queda compuesto por las siguientes 10 entidades persistentes:

```text
Usuario
Producto
Proveedor
Abastecimiento
DetalleAbastecimiento
MovimientoInventario
Venta
DetalleVenta
SesionCaja
MovimientoCaja
```

Cualquier modificación relevante de este modelo deberá coordinarse con el equipo antes de generar nuevas migraciones de Entity Framework Core.