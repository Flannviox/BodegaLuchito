##### \# Arquitectura - Bodega Luchito

##### 

##### \## Descripción

##### 

##### Bodega Luchito es una aplicación de escritorio local desarrollada

##### con C#, .NET 10 y WPF.

##### 

##### El sistema utiliza una arquitectura por capas con organización modular.

##### 

##### \## Capas

##### 

##### \### BodegaLuchito.Domain

##### 

##### Contiene los conceptos y reglas fundamentales del negocio.

##### 

##### Ejemplos:

##### \- Producto

##### \- Proveedor

##### \- Venta

##### \- DetalleVenta

##### \- MovimientoInventario

##### \- Usuario

##### \- SesionCaja

##### \- MovimientoCaja

##### 

##### Domain no debe depender de WPF, Entity Framework Core, SQLite

##### ni de otros proyectos de la solución.

##### 

##### \### BodegaLuchito.Application

##### 

##### Contiene los casos de uso y procesos de la aplicación.

##### 

##### Ejemplos:

##### \- Registrar producto

##### \- Registrar venta

##### \- Registrar abastecimiento

##### \- Consultar inventario

##### \- Realizar cierre de caja

##### 

##### Application depende de Domain.

##### 

##### \### BodegaLuchito.Infrastructure

##### 

##### Contiene las implementaciones relacionadas con tecnologías externas.

##### 

##### Ejemplos:

##### \- Entity Framework Core

##### \- SQLite

##### \- Repositorios

##### \- Exportación Excel

##### \- Impresión ESC/POS

##### 

##### Infrastructure puede depender de Application y Domain.

##### 

##### \### BodegaLuchito.Desktop

##### 

##### Contiene la interfaz gráfica desarrollada con WPF y MVVM.

##### 

##### Incluye:

##### \- Views

##### \- ViewModels

##### \- Controles

##### \- Navegación

##### \- Recursos visuales

##### 

##### Desktop consume los servicios de Application y realiza la composición

##### con Infrastructure.

##### 

##### \## Organización modular

##### 

##### El sistema se organiza internamente en los siguientes módulos:

##### 

##### \- Autenticacion

##### \- Productos

##### \- Proveedores

##### \- Abastecimientos

##### \- Inventario

##### \- Ventas

##### \- Caja

##### \- Reportes

##### \- BI

##### 

##### La generación de tickets pertenece funcionalmente al módulo Ventas.

##### 

##### Los módulos se organizan dentro de cada capa. No se crea un proyecto

##### .csproj independiente por cada módulo.

##### 

##### \## Comunicación entre módulos

##### 

##### Los módulos pueden depender funcionalmente de otros módulos,

##### pero deben evitar acceder directamente a su persistencia interna.

##### 

##### Las interacciones deben realizarse mediante contratos definidos

##### en Application.

##### 

##### Dependencias funcionales principales:

##### 

##### Productos → Inventario → Ventas

##### 

##### Proveedores → Abastecimientos

##### 

##### Abastecimientos → Inventario

##### 

##### Abastecimientos → Caja

##### 

##### Ventas → Inventario

##### 

##### Ventas → Caja

##### 

##### Ventas → Tickets

##### 

##### Autenticacion → operaciones que requieren usuario y trazabilidad

##### 

##### Operaciones → Reportes → BI

##### 

##### \## Base de datos

##### 

##### El sistema utilizará una única base de datos SQLite local.

##### 

##### La estructura de la base de datos será gestionada mediante

##### Entity Framework Core y migraciones.

