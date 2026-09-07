# \# Modelo de datos - Bodega Luchito

# 

# \## Entidades principales

# 

# \### Autenticacion

# 

# \- Usuario

# 

# \### Productos

# 

# \- Producto

# 

# \### Proveedores

# 

# \- Proveedor

# 

# \### Abastecimientos

# 

# \- Abastecimiento

# \- DetalleAbastecimiento

# 

# \### Inventario

# 

# \- LoteInventario

# \- MovimientoInventario

# 

# \### Ventas

# 

# \- Venta

# \- DetalleVenta

# 

# \### Caja

# 

# \- SesionCaja

# \- MovimientoCaja

# 

# \## Elementos sin tabla propia

# 

# \### Tickets

# 

# Los tickets internos se generan a partir de una venta confirmada.

# 

# \### Reportes

# 

# Se generan mediante consultas sobre las entidades existentes.

# 

# \### Business Intelligence

# 

# Los indicadores se calculan a partir de los datos operativos

# registrados por el sistema.

# 

# \## Consideraciones

# 

# Los productos pueden ser inventariables o no inventariables.

# 

# Los productos pueden venderse por unidad o por peso.

# 

# Los productos inventariables mantienen StockActual y un historial

# de movimientos de inventario.

# 

# Las ventas confirmadas generan ingresos de caja.

# 

# Los abastecimientos confirmados generan egresos de caja.

# 

# Los productos y proveedores se desactivan en lugar de eliminarse

# cuando poseen información histórica relacionada.

