# Ventas: presentación visual v1

Mejora visual de la pantalla de ventas sobre la rama de integración actual. Se conserva el
flujo existente de búsqueda, selección de productos, edición de cantidad, cobro y anulación.

## Nueva venta

- Buscador destacado para escribir el nombre o escanear el código de barras.
- Resultados con producto, código, existencia disponible y precio.
- Carrito con producto, precio por unidad o por kg, cantidad, importe y acción para quitar.
- Resumen lateral con cantidad de productos, total a cobrar y medios de pago disponibles.
- En ventanas angostas, el resumen se convierte en una franja inferior para no cortar la lista.
- Mensajes de ayuda escritos para la persona que atiende la bodega, sin atajos de teclado.

## Historial

- Estado visible como `Completada` o `Anulada`.
- Una venta anulada usa colores de alerta y conserva su importe para mantener la trazabilidad.
- Al seleccionar una venta anulada se muestra el motivo y la fecha de anulación.
- Se conserva el botón `Ver ticket`; `Anular` solo aparece cuando la venta pertenece al turno actual
  y todavía no está anulada.

## Alcance técnico

El cambio se limita a la presentación WPF y a un comando de limpieza de búsqueda. No agrega
paquetes, tablas, migraciones ni modifica las reglas de venta, inventario o caja.

## Comprobación

- `dotnet build` compila la solución.
- La vista real se comprobó con carrito, historial, ventana compacta y venta anulada seleccionada.
- No se detectaron errores de enlace WPF en esos escenarios.
- Conviene probar manualmente con una venta por unidad, una venta por kg, una venta anulada y
  una ventana reducida antes de fusionar.
