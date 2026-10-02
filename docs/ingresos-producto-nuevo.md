# Crear productos dentro de un ingreso

## Inventario inicial

En Inventario → Existencias, usar **Producto y saldo inicial**. Completar nombre, categoría, tipo de venta, precio de venta, stock mínimo, código opcional y cantidad.

**Registrar producto y saldo** guarda el producto y su movimiento inicial juntos. Si falla la validación, no queda un producto con stock cero. Cancelar no guarda nada. El botón Inventario inicial existente sigue disponible para productos ya creados que todavía sean elegibles.

No se requiere proveedor ni se registra un pago. El motivo del movimiento creado es “Mercadería existente al iniciar el sistema”.

## Abastecimiento

Usar **Nuevo producto para esta compra**, junto a la búsqueda de productos. Completar los datos del producto, cantidad y costo por unidad o por lote. El precio de venta es independiente del costo de compra.

**Añadir al abastecimiento** agrega una fila marcada como “(nuevo)”. Todavía no existe en el catálogo. No cambia el proveedor, el medio de pago ni las otras filas.

- Editar una fila nueva permite corregir tanto sus datos como cantidad y costo.
- Quitar esa fila descarta el producto pendiente.
- Confirmar el ingreso guarda los productos nuevos, la compra, los movimientos de stock y el egreso de Caja dentro de la misma transacción.
- Una compra fallida conserva el detalle para corregirlo y reintentar.
- Los detalles pendientes permanecen solo en la pantalla actual; todavía no hay guardado de borradores al cerrar la aplicación o salir del módulo.

Se mantiene la regla existente de Caja abierta para confirmar un abastecimiento.

## Validación

- Nombre y categoría obligatorios, precio positivo, stock mínimo no negativo.
- Código de barras opcional; si existe en el catálogo o se repite entre nuevos productos de la compra, se rechaza el registro.
- Cantidades enteras para unidades, hasta tres decimales para peso.
- En el formulario se acepta punto o coma decimal, sin separadores de miles.
- Costo por unidad: hasta cuatro decimales; costo total del lote: hasta dos. Se conserva el costo unitario digitado y, en modo lote, el total digitado.
- Al confirmar se vuelve a comprobar que la categoría siga activa.

## Pruebas

Compilación correcta y 96 pruebas automatizadas aprobadas. Los nuevos casos verifican:

1. Producto y saldo inicial guardados juntos con referencia de movimiento correcta y sin pago.
2. Cantidad inválida sin producto huérfano.
3. Código repetido sin duplicar producto ni movimiento.
4. Compra con dos productos nuevos sin código y uno existente: stock, compra y pago correctos.
5. Compra con Caja cerrada o códigos duplicados: ningún registro parcial.

También se ejecutaron siete comprobaciones del modelo del formulario: cancelar, cantidad inválida, confirmar, conservar costo unitario, aceptar decimales, precargar edición y mantener el formulario abierto tras un fallo.

Se revisó el XAML con datos de ejemplo en 1100×660 y 720×440. En el tamaño reducido se desplaza el contenido manteniendo los botones visibles.

Prueba manual recomendada: elegir un proveedor, añadir un producto existente y uno nuevo, editar el nuevo, comprobar el total y confirmar. Revisar después Productos, Movimientos e Historial de abastecimientos. Repetir quitando la fila nueva antes de confirmar para comprobar que no se cree.

No se modifica el esquema de la base de datos ni se agrega una migración.

## Archivos afectados

- Application/Productos/UseCases/PrepararProductoNuevo.cs y RegistrarProductoUseCase.cs: reglas comunes.
- Application/Inventario/DTOs/RegistrarMovimientoManualRequest.cs: alta y saldo en una solicitud.
- Application/Abastecimiento/DTOs/RegistrarAbastecimientoRequest.cs y UseCases/RegistrarAbastecimientoUseCase.cs: productos nuevos pendientes.
- Infrastructure/Productos/Repositories/AltaProductoEnOperacion.cs: validación y alta dentro de la transacción existente.
- Infrastructure/Inventario/Repositories/InventarioRepository.cs y Infrastructure/Abastecimiento/Repositories/AbastecimientoRepository.cs: guardado conjunto.
- Desktop/Modules/Productos/ViewModels/NuevoProductoIngresoViewModel.cs y Views/NuevoProductoIngresoView.xaml(.cs): formulario compartido.
- Desktop/Modules/Inventario y Desktop/Modules/Abastecimiento: conexión del formulario, edición de pendientes y botones.
- Tests/Application/Inventario/InventarioOperativoTests.cs y Tests/Application/Abastecimiento/RegistrarAbastecimientoUseCaseTests.cs: casos de persistencia y validación.
