# Inventario operativo v1

Acceso desde **Inventario**, con sesión de administradora. Productos sigue administrando el catálogo; Abastecimiento registra compras a proveedores.

## Operaciones

- **Existencias:** búsqueda por nombre o código, filtros de categoría y estado, stock actual, mínimo y unidad. Los indicadores resumen todos los productos, independientemente del filtro.
- **Inventario inicial:** mercadería que ya existía al comenzar a usar el sistema. Solo productos activos con stock cero, sin movimientos ni compras previas. No requiere proveedor, costo ni pago. Agotar un producto no habilita otro saldo inicial.
- **Ajustar por conteo:** ingresar la cantidad total encontrada físicamente; el sistema registra la diferencia, positiva o negativa. No usarlo para registrar una compra.
- **Pérdida o daño:** ingresar cuánto se retira. No puede superar las existencias.
- **Movimientos:** historial de abastecimientos, saldos iniciales, conteos y pérdidas. Incluye productos desactivados; permite filtrar por fechas, tipo, producto y motivo. Seleccionar una fila muestra su motivo y la referencia a Abastecimiento, si corresponde.

Cantidades: enteras por unidad; hasta tres decimales por kilogramo. Se acepta punto o coma decimal, sin separadores de miles. Los movimientos manuales requieren motivo, guardan fecha, responsable, stock anterior y posterior, y no modifican Caja ni el precio de venta.

Stock y movimiento se guardan juntos en una transacción. Si el stock cambió después de abrir el formulario, se rechaza el registro: cancelar, actualizar la lista y revisar nuevamente. Los movimientos confirmados no se editan ni eliminan; una corrección se registra como nuevo ajuste con motivo.

## Base de datos e integración

No se modifica el esquema ni se necesita una migración nueva: se reutiliza MovimientosInventario. Las migraciones previas deben estar aplicadas mediante el inicio normal de la aplicación.

Abastecimiento conserva su consulta de ingresos por compra. Inventario ofrece la consulta conjunta sin el límite de 200 filas del historial anterior. Ventas todavía debe integrar sus salidas cuando se desarrolle ese flujo; esta entrega no implementa ventas, devoluciones ni valoración monetaria de existencias.

Los registros históricos anteriores se muestran tal como existen. No se inventan movimientos para productos con stock antiguo sin trazabilidad.

## Verificación

Desde la raíz del proyecto:

```powershell
dotnet build
dotnet test
```

Prueba manual en una base de prueba:

1. Crear en Productos un producto de tipo Peso, con stock cero.
2. En Inventario registrar saldo inicial de 80.125 kg. Comprobar el movimiento y el responsable.
3. Registrar un conteo de 78 kg, con motivo. La variación debe ser -2.125.
4. Registrar una pérdida de 0.5 kg. El stock debe quedar en 77.5.
5. Cerrar y abrir la aplicación. Verificar los tres movimientos y el mismo stock.
6. Intentar retirar 100 kg: debe rechazarse sin modificar stock ni historial.
7. Probar un producto por unidad: rechazar 1.5 y aceptar un entero.
8. Llevar un producto a cero mediante conteo: no debe volver a admitir saldo inicial.
9. Registrar una compra en Abastecimiento: debe aparecer en ambos historiales, conservando sus cantidades.
10. Desactivar un producto desde Productos: su historial permanece; no debe admitir operaciones manuales mientras esté desactivado.
11. Revisar filtros, rueda del ratón sobre las tablas, textos al pasar el cursor y ventana reducida. Las tablas tienen desplazamiento propio; en ventanas pequeñas, el formulario permite desplazarse manteniendo Confirmar y Cancelar visibles.

Se renderizó el XAML con datos de ejemplo a 1200×680 y 700×440. Esto comprueba distribución estática; el recorrido interactivo en la aplicación debe revisarse también.

## Archivos principales

- Domain/Inventario: reglas y tipos de movimiento.
- Application/Inventario: contratos, consultas y registro con validación de sesión.
- Infrastructure/Inventario/Repositories/InventarioRepository.cs: consulta y transacción de stock.
- Desktop/Modules/Inventario: vista y modelo de vista.
- Desktop/Modules/Abastecimiento/ViewModels/AbastecimientoViewModel.cs: filtro de entradas en su historial.
- Desktop/App.xaml.cs, MainWindow.xaml, Shell/ViewModels/MainWindowViewModel.cs y Resources/ViewTemplates.xaml: registro y navegación.
- Infrastructure/DependencyInjection.cs: registro del caso de uso.
- tests/BodegaLuchito.Tests/Application/Inventario/InventarioOperativoTests.cs: persistencia, validaciones, permisos, historial y compatibilidad con compras antiguas.
