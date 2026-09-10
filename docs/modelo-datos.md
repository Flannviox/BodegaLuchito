##### \# Modelo de datos - Bodega Luchito

##### 

##### \## 1. Propósito

##### 

##### Este documento define el modelo de datos base que utilizará el sistema Bodega Luchito.

##### 

##### Su objetivo es servir como referencia común para el equipo de desarrollo y evitar que cada módulo defina entidades, tipos de datos o relaciones diferentes.

##### 

##### El modelo inicial contiene 10 entidades persistentes:

##### 

##### 1\. Usuario

##### 2\. Producto

##### 3\. Proveedor

##### 4\. Abastecimiento

##### 5\. DetalleAbastecimiento

##### 6\. MovimientoInventario

##### 7\. Venta

##### 8\. DetalleVenta

##### 9\. SesionCaja

##### 10\. MovimientoCaja

##### 

##### Los módulos Tickets, Reportes y Business Intelligence no requieren tablas propias en esta versión.

##### 

##### \---

##### 

##### \# 2. Autenticacion

##### 

##### \## 2.1 Usuario

##### 

##### Representa a una persona autorizada para utilizar el sistema.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | NombreCompleto | string | Sí | Nombre del usuario |

##### | NombreUsuario | string | Sí | Nombre utilizado para iniciar sesión |

##### | PasswordHash | string | Sí | Contraseña almacenada mediante hash |

##### | Rol | RolUsuario | Sí | Rol del usuario |

##### | Activo | bool | Sí | Indica si puede utilizar el sistema |

##### | FechaCreacion | DateTime | Sí | Fecha de creación |

##### | FechaActualizacion | DateTime? | No | Última modificación |

##### 

##### \### Restricciones

##### 

##### \- `NombreUsuario` debe ser único.

##### \- La contraseña nunca debe almacenarse en texto plano.

##### \- Un usuario con información histórica no debe eliminarse físicamente.

##### \- Para deshabilitarlo se utiliza `Activo = false`.

##### 

##### \### RolUsuario

##### 

##### ```csharp

##### public enum RolUsuario

##### {

##### &#x20;   Administradora = 1,

##### &#x20;   Vendedora = 2

##### }

##### ```

##### 

##### \---

##### 

##### \# 3. Productos

##### 

##### \## 3.1 Producto

##### 

##### Representa un producto que puede ser vendido por la bodega.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | Nombre | string | Sí | Nombre del producto |

##### | Categoria | string | Sí | Categoría comercial |

##### | CodigoBarras | string? | No | Código utilizado por el lector |

##### | PrecioVenta | decimal | Sí | Precio actual de venta |

##### | UnidadVenta | UnidadVenta | Sí | Unidad o peso |

##### | ControlaInventario | bool | Sí | Indica si se controla stock |

##### | StockActual | decimal | Sí | Stock disponible |

##### | StockMinimo | decimal | Sí | Nivel mínimo esperado |

##### | Activo | bool | Sí | Indica si puede utilizarse |

##### | FechaCreacion | DateTime | Sí | Fecha de registro |

##### | FechaActualizacion | DateTime? | No | Última modificación |

##### 

##### \### Restricciones

##### 

##### \- `Nombre` es obligatorio.

##### \- `Categoria` es obligatoria.

##### \- `PrecioVenta` debe ser mayor que cero.

##### \- `CodigoBarras` es opcional.

##### \- Cuando exista, `CodigoBarras` debe ser único.

##### \- `StockActual` no puede ser negativo.

##### \- `StockMinimo` no puede ser negativo.

##### \- Si `ControlaInventario = false`:

##### 

##### ```text

##### StockActual = 0

##### StockMinimo = 0

##### ```

##### 

##### \- Un producto con movimientos históricos no debe eliminarse físicamente.

##### \- Para retirarlo de uso se utiliza `Activo = false`.

##### 

##### \### UnidadVenta

##### 

##### ```csharp

##### public enum UnidadVenta

##### {

##### &#x20;   Unidad = 1,

##### &#x20;   Peso = 2

##### }

##### ```

##### 

##### \### Productos por peso

##### 

##### Las cantidades de productos vendidos por peso se almacenarán utilizando `decimal`.

##### 

##### La unidad base para el sistema será el kilogramo.

##### 

##### Ejemplo:

##### 

##### ```text

##### 350 gramos = 0.350 kg

##### ```

##### 

##### Si el precio es:

##### 

##### ```text

##### S/ 8.00 por kg

##### ```

##### 

##### el subtotal será:

##### 

##### ```text

##### 0.350 × 8.00 = S/ 2.80

##### ```

##### 

##### \---

##### 

##### \# 4. Proveedores

##### 

##### \## 4.1 Proveedor

##### 

##### Representa a una persona o empresa que suministra productos a la bodega.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | Nombre | string | Sí | Nombre o razón comercial |

##### | Ruc | string? | No | RUC del proveedor |

##### | Telefono | string? | No | Número de contacto |

##### | Direccion | string? | No | Dirección |

##### | Activo | bool | Sí | Indica si está disponible |

##### | FechaCreacion | DateTime | Sí | Fecha de registro |

##### | FechaActualizacion | DateTime? | No | Última modificación |

##### 

##### \### Restricciones

##### 

##### \- `Nombre` es obligatorio.

##### \- `Ruc` es opcional.

##### \- Cuando exista, el RUC debe contener exactamente 11 dígitos.

##### \- Cuando exista, el RUC no debe repetirse.

##### \- Un proveedor con abastecimientos históricos no debe eliminarse.

##### \- Para deshabilitarlo se utiliza `Activo = false`.

##### 

##### \---

##### 

##### \# 5. Abastecimientos

##### 

##### \## 5.1 Abastecimiento

##### 

##### Representa la recepción de productos provenientes de un proveedor.

##### 

##### En Bodega Luchito el proveedor se paga al momento de recibir la mercadería, por lo que un abastecimiento confirmado también origina un egreso de caja.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | ProveedorId | int | Sí | FK → Proveedor |

##### | UsuarioId | int | Sí | FK → Usuario que registra |

##### | SesionCajaId | int | Sí | FK → SesionCaja |

##### | FechaHora | DateTime | Sí | Fecha y hora del abastecimiento |

##### | MetodoPago | MetodoPago | Sí | Medio utilizado para pagar |

##### | Total | decimal | Sí | Importe total |

##### 

##### \### Restricciones

##### 

##### \- Debe existir un proveedor válido.

##### \- Debe contener al menos un detalle.

##### \- `Total` debe ser mayor que cero.

##### \- Debe registrarse el usuario responsable.

##### \- El abastecimiento confirmado genera un movimiento de caja.

##### \- Los productos inventariables generan movimientos de entrada de inventario.

##### 

##### \---

##### 

##### \## 5.2 DetalleAbastecimiento

##### 

##### Representa cada producto incluido dentro de un abastecimiento.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | AbastecimientoId | int | Sí | FK → Abastecimiento |

##### | ProductoId | int | Sí | FK → Producto |

##### | Cantidad | decimal | Sí | Cantidad recibida |

##### | CostoUnitario | decimal | Sí | Costo de adquisición por unidad |

##### | Subtotal | decimal | Sí | Cantidad × CostoUnitario |

##### 

##### \### Restricciones

##### 

##### \- `Cantidad` debe ser mayor que cero.

##### \- `CostoUnitario` no puede ser negativo.

##### \- `Subtotal` corresponde a:

##### 

##### ```text

##### Cantidad × CostoUnitario

##### ```

##### 

##### \### Relación

##### 

##### ```text

##### Abastecimiento 1 ───── N DetalleAbastecimiento

##### 

##### Producto        1 ───── N DetalleAbastecimiento

##### ```

##### 

##### \---

##### 

##### \# 6. Inventario

##### 

##### \## 6.1 MovimientoInventario

##### 

##### Representa cualquier entrada, salida, ajuste o reversión de stock.

##### 

##### `Producto.StockActual` mantiene el estado actual.

##### 

##### `MovimientoInventario` mantiene el historial que explica cómo se llegó a ese stock.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | ProductoId | int | Sí | FK → Producto |

##### | UsuarioId | int | Sí | FK → Usuario responsable |

##### | Tipo | TipoMovimientoInventario | Sí | Tipo de movimiento |

##### | Cantidad | decimal | Sí | Cantidad involucrada |

##### | StockAnterior | decimal | Sí | Stock antes del movimiento |

##### | StockPosterior | decimal | Sí | Stock después del movimiento |

##### | FechaHora | DateTime | Sí | Fecha y hora |

##### | Motivo | string? | No | Motivo, principalmente para ajustes |

##### | VentaId | int? | No | FK → Venta cuando corresponda |

##### | AbastecimientoId | int? | No | FK → Abastecimiento cuando corresponda |

##### 

##### \### TipoMovimientoInventario

##### 

##### ```csharp

##### public enum TipoMovimientoInventario

##### {

##### &#x20;   EntradaAbastecimiento = 1,

##### &#x20;   SalidaVenta = 2,

##### &#x20;   AjusteEntrada = 3,

##### &#x20;   AjusteSalida = 4,

##### &#x20;   ReversionVenta = 5

##### }

##### ```

##### 

##### \### Ejemplo

##### 

##### ```text

##### Producto: Coca Cola 500 ml

##### 

##### StockAnterior  = 20

##### Tipo           = SalidaVenta

##### Cantidad       = 2

##### StockPosterior = 18

##### VentaId        = 150

##### ```

##### 

##### \### Reglas

##### 

##### \- `Cantidad` se almacena como un valor positivo.

##### \- `Tipo` determina si representa entrada o salida.

##### \- Solo se generan movimientos para productos con:

##### 

##### ```text

##### ControlaInventario = true

##### ```

##### 

##### \- Una modificación de stock debe actualizar `StockActual` y crear su `MovimientoInventario` dentro de la misma operación.

##### 

##### \---

##### 

##### \# 7. Ventas

##### 

##### \## 7.1 Venta

##### 

##### Representa una venta confirmada en el punto de venta.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | UsuarioId | int | Sí | FK → Usuario que registra |

##### | SesionCajaId | int | Sí | FK → SesionCaja |

##### | FechaHora | DateTime | Sí | Fecha y hora |

##### | MetodoPago | MetodoPago | Sí | Efectivo, Yape o Plin |

##### | Total | decimal | Sí | Total de la venta |

##### | Estado | EstadoVenta | Sí | Confirmada o anulada |

##### | FechaAnulacion | DateTime? | No | Fecha de anulación |

##### | UsuarioAnulacionId | int? | No | FK → Usuario que anula |

##### | MotivoAnulacion | string? | No | Motivo de la anulación |

##### 

##### \### EstadoVenta

##### 

##### ```csharp

##### public enum EstadoVenta

##### {

##### &#x20;   Confirmada = 1,

##### &#x20;   Anulada = 2

##### }

##### ```

##### 

##### \### Reglas

##### 

##### \- Una venta debe contener al menos un detalle.

##### \- `Total` debe ser mayor que cero.

##### \- Una venta confirmada genera un ingreso en caja.

##### \- Los productos inventariables generan salidas de inventario.

##### \- Una venta nunca debe eliminarse físicamente.

##### \- Una anulación cambia su estado a `Anulada`.

##### \- Una anulación debe conservar los detalles originales.

##### \- La anulación debe registrar usuario, fecha y motivo.

##### 

##### \---

##### 

##### \## 7.2 DetalleVenta

##### 

##### Representa cada producto incluido en una venta.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | VentaId | int | Sí | FK → Venta |

##### | ProductoId | int | Sí | FK → Producto |

##### | NombreProducto | string | Sí | Nombre histórico |

##### | UnidadVenta | UnidadVenta | Sí | Unidad utilizada al vender |

##### | Cantidad | decimal | Sí | Cantidad vendida |

##### | PrecioUnitario | decimal | Sí | Precio al momento de venta |

##### | Subtotal | decimal | Sí | Cantidad × PrecioUnitario |

##### 

##### \### Información histórica

##### 

##### `NombreProducto`, `UnidadVenta` y `PrecioUnitario` se conservan dentro del detalle para mantener correctamente el historial.

##### 

##### Ejemplo:

##### 

##### ```text

##### Septiembre:

##### Producto = Coca Cola

##### Precio = S/ 3.50

##### 

##### Octubre:

##### Producto = Coca Cola

##### Precio actual = S/ 4.00

##### ```

##### 

##### La venta realizada en septiembre debe continuar mostrando:

##### 

##### ```text

##### PrecioUnitario = S/ 3.50

##### ```

##### 

##### \### Relación

##### 

##### ```text

##### Venta    1 ───── N DetalleVenta

##### 

##### Producto 1 ───── N DetalleVenta

##### ```

##### 

##### \---

##### 

##### \# 8. Caja

##### 

##### \## 8.1 SesionCaja

##### 

##### Representa una apertura y cierre de caja.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | UsuarioAperturaId | int | Sí | FK → Usuario |

##### | FechaApertura | DateTime | Sí | Inicio de la sesión |

##### | FondoInicial | decimal | Sí | Fondo inicial en efectivo |

##### | Estado | EstadoSesionCaja | Sí | Abierta o cerrada |

##### | UsuarioCierreId | int? | No | FK → Usuario que cierra |

##### | FechaCierre | DateTime? | No | Fecha y hora de cierre |

##### | EfectivoEsperado | decimal? | No | Importe calculado por sistema |

##### | EfectivoReal | decimal? | No | Importe comprobado físicamente |

##### | DiferenciaEfectivo | decimal? | No | Real - Esperado |

##### | YapeEsperado | decimal? | No | Yape esperado |

##### | YapeReal | decimal? | No | Yape verificado |

##### | DiferenciaYape | decimal? | No | Real - Esperado |

##### | PlinEsperado | decimal? | No | Plin esperado |

##### | PlinReal | decimal? | No | Plin verificado |

##### | DiferenciaPlin | decimal? | No | Real - Esperado |

##### | ObservacionCierre | string? | No | Comentario de cierre |

##### 

##### \### EstadoSesionCaja

##### 

##### ```csharp

##### public enum EstadoSesionCaja

##### {

##### &#x20;   Abierta = 1,

##### &#x20;   Cerrada = 2

##### }

##### ```

##### 

##### \### Reglas

##### 

##### \- Solo puede existir una sesión de caja abierta a la vez.

##### \- `FondoInicial` debe ser mayor o igual a cero.

##### \- El fondo inicial no debe estar hardcodeado.

##### \- El valor actual habitual de S/ 1000 se ingresará al momento de abrir caja.

##### \- Al cerrar se calculan valores esperados y diferencias.

##### \- Una sesión cerrada no debe volver a recibir movimientos.

##### 

##### \### Cálculo básico

##### 

##### Para efectivo:

##### 

##### ```text

##### EfectivoEsperado =

##### FondoInicial

##### \+ Ingresos en efectivo

##### \- Egresos en efectivo

##### \- Reversiones correspondientes

##### ```

##### 

##### Para Yape y Plin:

##### 

##### ```text

##### Esperado =

##### Ingresos

##### \- Egresos

##### \- Reversiones correspondientes

##### ```

##### 

##### Las diferencias se calculan como:

##### 

##### ```text

##### Diferencia = ValorReal - ValorEsperado

##### ```

##### 

##### \---

##### 

##### \## 8.2 MovimientoCaja

##### 

##### Representa un ingreso, egreso o reversión monetaria dentro de una sesión de caja.

##### 

##### | Campo | Tipo C# | Requerido | Descripción |

##### |---|---|---:|---|

##### | Id | int | Sí | Identificador único |

##### | SesionCajaId | int | Sí | FK → SesionCaja |

##### | UsuarioId | int | Sí | FK → Usuario |

##### | Tipo | TipoMovimientoCaja | Sí | Tipo de movimiento |

##### | MetodoPago | MetodoPago | Sí | Efectivo, Yape o Plin |

##### | Monto | decimal | Sí | Importe |

##### | FechaHora | DateTime | Sí | Fecha y hora |

##### | VentaId | int? | No | FK → Venta |

##### | AbastecimientoId | int? | No | FK → Abastecimiento |

##### | Descripcion | string? | No | Información adicional |

##### 

##### \### TipoMovimientoCaja

##### 

##### ```csharp

##### public enum TipoMovimientoCaja

##### {

##### &#x20;   IngresoVenta = 1,

##### &#x20;   EgresoAbastecimiento = 2,

##### &#x20;   ReversionVenta = 3

##### }

##### ```

##### 

##### \### Reglas

##### 

##### \- `Monto` debe ser mayor que cero.

##### \- El signo no determina la operación.

##### \- El campo `Tipo` determina si representa ingreso, egreso o reversión.

##### \- Una venta confirmada genera `IngresoVenta`.

##### \- Un abastecimiento confirmado genera `EgresoAbastecimiento`.

##### \- Una venta anulada genera la reversión correspondiente.

##### 

##### \---

##### 

##### \# 9. Enums compartidos

##### 

##### \## MetodoPago

##### 

##### Este enum pertenece a un espacio compartido porque es utilizado por Ventas, Caja y Abastecimientos.

##### 

##### Ubicación sugerida:

##### 

##### ```text

##### BodegaLuchito.Domain/

##### └── Shared/

##### &#x20;   └── Enums/

##### &#x20;       └── MetodoPago.cs

##### ```

##### 

##### Definición:

##### 

##### ```csharp

##### public enum MetodoPago

##### {

##### &#x20;   Efectivo = 1,

##### &#x20;   Yape = 2,

##### &#x20;   Plin = 3

##### }

##### ```

##### 

##### \---

##### 

##### \# 10. Resumen de relaciones

##### 

##### ```text

##### Usuario

##### &#x20;  │

##### &#x20;  ├──── 1:N ──── Venta

##### &#x20;  ├──── 1:N ──── Abastecimiento

##### &#x20;  ├──── 1:N ──── MovimientoInventario

##### &#x20;  ├──── 1:N ──── MovimientoCaja

##### &#x20;  └──── 1:N ──── SesionCaja

##### 

##### 

##### Proveedor

##### &#x20;  │

##### &#x20;  └──── 1:N ──── Abastecimiento

##### 

##### 

##### Abastecimiento

##### &#x20;  │

##### &#x20;  ├──── 1:N ──── DetalleAbastecimiento

##### &#x20;  ├──── 1:N ──── MovimientoInventario

##### &#x20;  └──── 1:N ──── MovimientoCaja

##### 

##### 

##### Producto

##### &#x20;  │

##### &#x20;  ├──── 1:N ──── DetalleVenta

##### &#x20;  ├──── 1:N ──── DetalleAbastecimiento

##### &#x20;  └──── 1:N ──── MovimientoInventario

##### 

##### 

##### Venta

##### &#x20;  │

##### &#x20;  ├──── 1:N ──── DetalleVenta

##### &#x20;  ├──── 1:N ──── MovimientoInventario

##### &#x20;  └──── 1:N ──── MovimientoCaja

##### 

##### 

##### SesionCaja

##### &#x20;  │

##### &#x20;  ├──── 1:N ──── Venta

##### &#x20;  ├──── 1:N ──── Abastecimiento

##### &#x20;  └──── 1:N ──── MovimientoCaja

##### ```

##### 

##### \---

##### 

##### \# 11. Flujos que afectan varias entidades

##### 

##### \## 11.1 Confirmar venta

##### 

##### ```text

##### Confirmar Venta

##### &#x20;     │

##### &#x20;     ├── Crear Venta

##### &#x20;     │

##### &#x20;     ├── Crear DetalleVenta

##### &#x20;     │

##### &#x20;     ├── Registrar MovimientoCaja

##### &#x20;     │      Tipo = IngresoVenta

##### &#x20;     │

##### &#x20;     └── Por cada producto inventariable:

##### &#x20;            │

##### &#x20;            ├── disminuir Producto.StockActual

##### &#x20;            │

##### &#x20;            └── crear MovimientoInventario

##### &#x20;                   Tipo = SalidaVenta

##### 

##### &#x20;            ↓

##### 

##### &#x20;     Confirmar transacción

##### &#x20;            ↓

##### &#x20;     Permitir generar Ticket

##### ```

##### 

##### La creación de Venta, detalles, stock y movimiento de caja debe realizarse como una operación transaccional.

##### 

##### La impresión del ticket ocurre después de confirmar correctamente la venta.

##### 

##### Si la impresora falla:

##### 

##### ```text

##### Venta               guardada

##### DetalleVenta         guardado

##### Inventario           actualizado

##### MovimientoCaja       guardado

##### Ticket físico        puede reintentarse

##### ```

##### 

##### La venta no debe revertirse únicamente porque falle la impresora.

##### 

##### \---

##### 

##### \## 11.2 Confirmar abastecimiento

##### 

##### ```text

##### Confirmar Abastecimiento

##### &#x20;         │

##### &#x20;         ├── Crear Abastecimiento

##### &#x20;         │

##### &#x20;         ├── Crear DetalleAbastecimiento

##### &#x20;         │

##### &#x20;         ├── Crear MovimientoCaja

##### &#x20;         │      Tipo = EgresoAbastecimiento

##### &#x20;         │

##### &#x20;         └── Por cada producto inventariable:

##### &#x20;                │

##### &#x20;                ├── aumentar Producto.StockActual

##### &#x20;                │

##### &#x20;                └── crear MovimientoInventario

##### &#x20;                       Tipo = EntradaAbastecimiento

##### 

##### &#x20;                ↓

##### 

##### &#x20;         Confirmar transacción

##### ```

##### 

##### \---

##### 

##### \## 11.3 Anular venta

##### 

##### ```text

##### Anular Venta

##### &#x20;    │

##### &#x20;    ├── Estado = Anulada

##### &#x20;    ├── FechaAnulacion

##### &#x20;    ├── UsuarioAnulacionId

##### &#x20;    ├── MotivoAnulacion

##### &#x20;    │

##### &#x20;    ├── Por cada producto inventariable:

##### &#x20;    │      │

##### &#x20;    │      ├── devolver stock

##### &#x20;    │      └── crear MovimientoInventario

##### &#x20;    │             Tipo = ReversionVenta

##### &#x20;    │

##### &#x20;    └── crear MovimientoCaja

##### &#x20;           Tipo = ReversionVenta

##### ```

##### 

##### La Venta y sus DetalleVenta originales permanecen almacenados.

##### 

##### \---

##### 

##### \# 12. Elementos sin tabla propia

##### 

##### \## 12.1 Tickets

##### 

##### Los tickets internos se generan a partir de:

##### 

##### ```text

##### Venta

##### \+

##### DetalleVenta

##### \+

##### datos comerciales necesarios

##### ```

##### 

##### No se crea una tabla `Ticket` en esta versión.

##### 

##### La impresión pertenece funcionalmente al módulo Ventas.

##### 

##### \---

##### 

##### \## 12.2 Reportes

##### 

##### Los reportes se generan mediante consultas sobre las entidades existentes.

##### 

##### Ejemplos:

##### 

##### ```text

##### Ventas por fecha

##### Ventas por producto

##### Ventas por medio de pago

##### Estado de inventario

##### Movimientos de inventario

##### Abastecimientos por proveedor

##### Movimientos de caja

##### Cierres de caja

##### ```

##### 

##### No existe una tabla `Reporte`.

##### 

##### \---

##### 

##### \## 12.3 Business Intelligence

##### 

##### Los indicadores se calculan utilizando información histórica registrada por los módulos operativos.

##### 

##### Ejemplos:

##### 

##### ```text

##### Productos más vendidos

##### Categorías más vendidas

##### Ventas por día

##### Ventas por medio de pago

##### Ingresos diarios

##### Productos con baja rotación

##### ```

##### 

##### No existe una tabla `BI` ni una tabla `Dashboard`.

##### 

##### \---

##### 

##### \# 13. Consideraciones generales

##### 

##### Los productos pueden ser inventariables o no inventariables.

##### 

##### Los productos pueden venderse por unidad o por peso.

##### 

##### Las cantidades se almacenan mediante `decimal`.

##### 

##### Los importes monetarios se almacenan mediante `decimal`.

##### 

##### Los productos inventariables mantienen:

##### 

##### ```text

##### Producto.StockActual

##### \+

##### historial de MovimientoInventario

##### ```

##### 

##### Los productos no inventariables pueden venderse normalmente, pero no generan movimientos de stock.

##### 

##### Las ventas confirmadas generan ingresos de caja.

##### 

##### Los abastecimientos confirmados generan egresos de caja.

##### 

##### Las ventas anuladas no se eliminan físicamente.

##### 

##### Los productos y proveedores con información histórica se desactivan en lugar de eliminarse.

##### 

##### Las operaciones importantes registran el usuario responsable cuando corresponde.

##### 

##### \---

##### 

##### \# 14. Funcionalidades consideradas para una etapa posterior

##### 

##### \## Lotes y vencimientos

##### 

##### La identificación de productos por lote y el control de fechas de vencimiento se consideran extensiones posteriores del modelo.

##### 

##### Por el momento no se implementarán:

##### 

##### \- control detallado por lotes;

##### \- estrategias FIFO o FEFO;

##### \- alertas automáticas de vencimiento;

##### \- selección automática de lotes durante una venta;

##### \- descuento de stock por lote.

##### 

##### Si posteriormente los requerimientos lo exigen, podrá incorporarse:

##### 

##### ```text

##### LoteInventario

##### ```

##### 

##### relacionado con:

##### 

##### ```text

##### Producto

##### Abastecimiento

##### DetalleAbastecimiento

##### ```

##### 

##### con posibles campos como:

##### 

##### ```text

##### Id

##### ProductoId

##### DetalleAbastecimientoId

##### FechaIngreso

##### FechaVencimiento

##### CantidadInicial

##### CantidadDisponible

##### Activo

##### ```

##### 

##### Esta extensión no forma parte del Sprint 01 ni debe implementarse actualmente.

##### 

##### \---

##### 

##### \# 15. Ubicación sugerida en Domain

##### 

##### ```text

##### BodegaLuchito.Domain/

##### │

##### ├── Autenticacion/

##### │   ├── Entities/

##### │   │   └── Usuario.cs

##### │   └── Enums/

##### │       └── RolUsuario.cs

##### │

##### ├── Productos/

##### │   ├── Entities/

##### │   │   └── Producto.cs

##### │   └── Enums/

##### │       └── UnidadVenta.cs

##### │

##### ├── Proveedores/

##### │   └── Entities/

##### │       └── Proveedor.cs

##### │

##### ├── Abastecimientos/

##### │   └── Entities/

##### │       ├── Abastecimiento.cs

##### │       └── DetalleAbastecimiento.cs

##### │

##### ├── Inventario/

##### │   ├── Entities/

##### │   │   └── MovimientoInventario.cs

##### │   └── Enums/

##### │       └── TipoMovimientoInventario.cs

##### │

##### ├── Ventas/

##### │   ├── Entities/

##### │   │   ├── Venta.cs

##### │   │   └── DetalleVenta.cs

##### │   ├── Enums/

##### │   │   └── EstadoVenta.cs

##### │   └── Tickets/

##### │

##### ├── Caja/

##### │   ├── Entities/

##### │   │   ├── SesionCaja.cs

##### │   │   └── MovimientoCaja.cs

##### │   └── Enums/

##### │       ├── EstadoSesionCaja.cs

##### │       └── TipoMovimientoCaja.cs

##### │

##### └── Shared/

##### &#x20;   └── Enums/

##### &#x20;       └── MetodoPago.cs

##### ```

##### 

##### \---

##### 

##### \# 16. Modelo v1

##### 

##### El modelo de datos v1 queda compuesto por las siguientes 10 entidades persistentes:

##### 

##### ```text

##### Usuario

##### Producto

##### Proveedor

##### Abastecimiento

##### DetalleAbastecimiento

##### MovimientoInventario

##### Venta

##### DetalleVenta

##### SesionCaja

##### MovimientoCaja

##### ```

##### 

##### Cualquier modificación relevante de este modelo deberá coordinarse con el equipo antes de generar nuevas migraciones de Entity Framework Core.

