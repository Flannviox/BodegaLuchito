# \# AGENTS.md - Bodega Luchito

# 

# \## Proyecto

# 

# Sistema local de información comercial para Bodega Luchito.

# 

# Aplicación de escritorio Windows utilizada en un único punto de venta.

# 

# \## Stack tecnológico

# 

# \- C# 14

# \- .NET 10

# \- WPF

# \- MVVM

# \- SQLite

# \- Entity Framework Core

# \- ClosedXML

# \- LiveCharts2

# \- xUnit

# \- Git / GitHub

# 

# \## Arquitectura

# 

# La solución utiliza arquitectura por capas con organización modular.

# 

# Proyectos:

# 

# \- BodegaLuchito.Domain

# \- BodegaLuchito.Application

# \- BodegaLuchito.Infrastructure

# \- BodegaLuchito.Desktop

# \- BodegaLuchito.Tests

# 

# \## Dependencias

# 

# Domain no depende de ningún otro proyecto interno.

# 

# Application depende de Domain.

# 

# Infrastructure puede depender de Application y Domain.

# 

# Desktop depende de Application e Infrastructure.

# 

# Tests puede probar Domain, Application y cuando corresponda Infrastructure.

# 

# \## Módulos

# 

# \- Autenticacion

# \- Productos

# \- Proveedores

# \- Abastecimientos

# \- Inventario

# \- Ventas

# \- Caja

# \- Reportes

# \- BI

# 

# Tickets forma parte funcionalmente de Ventas.

# 

# \## Reglas obligatorias

# 

# No colocar lógica de negocio en Views ni en code-behind de WPF.

# 

# No agregar referencias de Infrastructure, Desktop, EF Core o SQLite

# dentro de Domain.

# 

# No acceder directamente a SQLite desde Desktop.

# 

# No modificar manualmente la estructura de la base de datos cuando

# exista Entity Framework Core. Utilizar migraciones.

# 

# Mantener las funcionalidades dentro de su módulo correspondiente.

# 

# No crear proyectos .csproj adicionales sin una razón arquitectónica

# explícita.

# 

# No agregar paquetes NuGet sin explicar su necesidad.

# 

# No cambiar la arquitectura general del proyecto sin autorización.

# 

# \## Estilo

# 

# Los conceptos del negocio se escriben en español sin tildes

# en identificadores.

# 

# Utilizar PascalCase para clases, propiedades y métodos.

# 

# Utilizar camelCase para variables y parámetros.

# 

# Los métodos asincrónicos deben terminar en Async.

# 

# Nullable debe permanecer habilitado.

# 

# \## Git

# 

# main es estable.

# 

# develop es la rama de integración.

# 

# Las funcionalidades se desarrollan en ramas feature creadas

# desde develop.

# 

# Ejemplo:

# 

# feature/RF-PRO-01-registrar-producto

# 

# No realizar cambios directamente sobre main.

# 

# \## Validación

# 

# Antes de considerar terminada una modificación:

# 

# 1\. Compilar la solución.

# 2\. Ejecutar las pruebas relacionadas.

# 3\. Revisar que no existan errores de compilación.

# 4\. Mostrar claramente qué archivos fueron modificados.

# 

# \## Comandos

# 

# Desde la raíz del repositorio:

# 

# dotnet build

# 

# dotnet test

# 

# \## Documentación

# 

# Antes de realizar cambios arquitectónicos revisar:

# 

# docs/arquitectura.md

# docs/convenciones.md

# docs/git-workflow.md

