# Descuento de inventario al confirmar ventas

## Comportamiento

Agregar productos al carrito, editar cantidades o cancelar el cobro no modifica el stock. Al confirmar el medio de pago se consultan las existencias vigentes y se guardan la venta, sus detalles, el descuento y los movimientos en una única transacción.

Si falta stock, el producto está desactivado, el usuario está inactivo o la cantidad es inválida, no se guarda la venta ni se descuentan otros productos. Los productos por unidad requieren enteros; por peso admiten hasta tres decimales. Las líneas repetidas del mismo producto se validan por su cantidad acumulada y producen un movimiento conjunto.

En Inventario → Movimientos aparece el tipo **Venta**, con variación negativa, fecha, responsable, stock anterior/posterior y el motivo **Salida por venta #N**. El número queda en la descripción del movimiento. El filtro de Abastecimiento sigue mostrando únicamente sus entradas.

Al agotar un producto no se habilita otro saldo inicial. También se revisan los detalles de ventas antiguas que todavía no tienen un movimiento de inventario.

La pantalla de cobro bloquea todos los medios de pago mientras guarda. Una vez confirmada la venta se limpia el carrito antes de abrir el ticket; un error del ticket informa que la venta ya fue guardada, para evitar repetir el cobro. El total visible se actualiza al editar cantidades.

## Alcance

- Aplica a ventas confirmadas después de incorporar este cambio.
- No recalcula ventas históricas: el stock anterior puede incluir conteos o correcciones posteriores. Si está desactualizado, debe verificarse mediante conteo físico y ajuste.
- No modifica el esquema ni requiere una migración adicional. La migración AgregarVentas incorporada desde develop debe aplicarse mediante el inicio normal de la aplicación.
- Esta integración cubre salidas por venta. El proceso de anulación/devolución deberá registrar su entrada compensatoria antes de habilitarse para uso. No se agregó esa pantalla.
- La integración del ingreso de dinero de Ventas con Caja sigue siendo un trabajo independiente; este cambio no registra movimientos de Caja ni cambia las reglas de cobro existentes.

## Verificación

Compilación y suite de 111 pruebas aprobadas. Se verificaron ventas mixtas, cantidades por peso, falta de stock, desactivados, líneas repetidas, persistencia al reabrir, productos antiguos sin marca inventariable y dos ventas concurrentes disputando el stock.

Una prueba fuerza un error al insertar el movimiento después de guardar la venta dentro de la transacción: se comprueba que se reviertan venta, detalles y stock.

Además se verificó el modelo de la pantalla con repositorios simulados: el carrito no descuenta, el total cambia al editar y pulsar distintos medios de pago mientras el primero está pendiente solo inicia un registro.

Prueba manual:

1. Anotar el stock de un producto, por ejemplo 10 unidades.
2. Agregar 3 al carrito y cancelar el cobro: el stock debe continuar en 10.
3. Confirmar la venta de 3: debe quedar en 7.
4. Entrar en Inventario → Movimientos → Venta: comprobar variación -3, 10 → 7 y número de venta.
5. Intentar vender 8 unidades: debe rechazarse conservando las 7 y sin nueva venta.
6. Repetir con un producto por peso; cerrar y abrir la aplicación para comprobar persistencia.

## Archivos afectados

- Application/Inventario/UseCases/PrepararSalidaVentaUseCase.cs: contrato de preparación de salidas.
- Domain/Inventario/Enums/TipoMovimientoInventario.cs: tipo Venta.
- Application/Inventario/DTOs/MovimientoInventarioDetalle.cs: descripción para el historial.
- Application/Ventas/UseCases/RegistrarVentaUseCase.cs: validación de identificadores.
- Infrastructure/Ventas/Repositories/VentaRepository.cs: transacción de venta y stock.
- Infrastructure/Inventario/Repositories/InventarioRepository.cs: saldo inicial excluye productos con ventas previas.
- Desktop/Modules/Inventario/ViewModels/InventarioViewModel.cs: filtro Venta.
- Desktop/Modules/Ventas/ViewModels/VentasViewModel.cs y Views/VentasView.xaml: protección del cobro, ticket y actualización de totales/catálogo.
- tests/BodegaLuchito.Tests/Application/Ventas/VentaInventarioTests.cs: pruebas de integración.
