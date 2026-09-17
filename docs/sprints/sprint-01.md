##### \# Sprint 01 - Ola 1 funcional

##### 

##### \## Fecha límite

##### 

##### Domingo 13 de septiembre de 2026.

##### 

##### \## Objetivo

##### 

##### Implementar en paralelo las primeras bases funcionales del sistema.

##### 

##### Asignación:

##### 

##### Flavio:

##### Productos.

##### 

##### Jesús:

##### Proveedores.

##### 

##### Alejandro:

##### Caja.

##### 

##### Autenticacion se desarrolla en paralelo por otro integrante.

##### 

##### \## Reglas comunes

##### 

##### Todas las ramas deben crearse desde develop.

##### 

##### No trabajar directamente en main ni develop.

##### 

##### Antes de iniciar:

##### 

##### git fetch

##### git pull

##### dotnet build

##### dotnet test

##### 

##### No generar migraciones de Entity Framework durante el desarrollo

##### individual de este sprint.

##### 

##### No modificar sin coordinación previa:

##### 

##### \- App.xaml.cs

##### \- MainWindow.xaml

##### \- MainWindow.xaml.cs

##### \- MainWindowViewModel.cs

##### \- Resources/ViewTemplates.xaml

##### \- Infrastructure/DependencyInjection.cs

##### \- Persistence/Migrations/

##### \- BodegaLuchitoDbContextModelSnapshot.cs

##### 

##### Cada desarrollador debe limitar sus cambios principalmente

##### a su módulo.

##### 

##### Antes del Pull Request:

##### 

##### dotnet build

##### dotnet test

##### 

##### El Pull Request debe dirigirse a develop.

##### 

##### \---

##### 

##### \# Flavio - Productos

##### 

##### \## Rama

##### 

##### feature/productos-sprint01

##### 

##### \## Objetivo

##### 

##### Implementar el registro básico de productos.

##### 

##### \## Application

##### 

##### Crear:

##### 

##### Application/Productos/Interfaces/IProductoRepository.cs

##### 

##### Application/Productos/DTOs/RegistrarProductoRequest.cs

##### 

##### Application/Productos/UseCases/RegistrarProductoUseCase.cs

##### 

##### \## Validaciones

##### 

##### Nombre requerido.

##### 

##### Categoria requerida.

##### 

##### PrecioVenta debe ser mayor que cero.

##### 

##### StockActual no puede ser negativo.

##### 

##### StockMinimo no puede ser negativo.

##### 

##### CodigoBarras puede ser null.

##### 

##### No permitir CodigoBarras duplicado.

##### 

##### Si ControlaInventario es false:

##### 

##### StockActual = 0

##### StockMinimo = 0

##### 

##### \## Infrastructure

##### 

##### Crear:

##### 

##### Infrastructure/Productos/Repositories/ProductoRepository.cs

##### 

##### Implementar:

##### 

##### \- agregar producto,

##### \- comprobar CodigoBarras duplicado,

##### \- listar productos activos.

##### 

##### No generar migración.

##### 

##### \## Desktop

##### 

##### Continuar:

##### 

##### Desktop/Modules/Productos

##### 

##### Implementar formulario básico de registro.

##### 

##### No modificar todavía navegación global.

##### 

##### \## Tests mínimos

##### 

##### \- producto válido se registra,

##### \- nombre vacío falla,

##### \- precio inválido falla,

##### \- código duplicado falla,

##### \- producto no inventariable queda con stock cero.

##### 

##### \---

##### 

##### \# Jesus - Proveedores

##### 

##### \## Rama

##### 

##### feature/proveedores-sprint01

##### 

##### \## Objetivo

##### 

##### Implementar el maestro básico de proveedores.

##### 

##### \## Domain

##### 

##### Crear Proveedor con:

##### 

##### Id: int

##### 

##### Nombre: string

##### 

##### Ruc: string

##### 

##### Telefono: string?

##### 

##### Direccion: string?

##### 

##### Activo: bool

##### 

##### FechaCreacion: DateTime

##### 

##### FechaActualizacion: DateTime?

##### 

##### \## Application

##### 

##### Crear:

##### 

##### IProveedorRepository

##### 

##### RegistrarProveedorRequest

##### 

##### RegistrarProveedorUseCase

##### 

##### \## Validaciones

##### 

##### Nombre requerido.

##### 

##### RUC obligatorio (regla actualizada el 16/09/2026).

##### 

##### RUC debe contener exactamente 11 dígitos; teléfono opcional de máximo 10 dígitos.

##### 

##### No permitir RUC duplicado.

##### 

##### \## Infrastructure

##### 

##### Crear:

##### 

##### ProveedorRepository

##### 

##### ProveedorConfiguration

##### 

##### No generar migración.

##### 

##### \## Desktop

##### 

##### Crear:

##### 

##### Modules/Proveedores/Views/ProveedoresView.xaml

##### 

##### Modules/Proveedores/ViewModels/ProveedoresViewModel.cs

##### 

##### No modificar navegación global.

##### 

##### \## Tests mínimos

##### 

##### \- proveedor válido se registra,

##### \- nombre vacío falla,

##### \- RUC inválido falla,

##### \- RUC duplicado falla,

##### \- proveedor nuevo queda activo.

##### 

##### \---

##### 

##### \# Alejandro - Caja

##### 

##### \## Rama

##### 

##### feature/caja-sprint01

##### 

##### \## Objetivo

##### 

##### Implementar la base de apertura y cierre de caja.

##### 

##### No integrar todavía con Ventas ni Abastecimientos.

##### 

##### \## Domain

##### 

##### Crear:

##### 

##### SesionCaja

##### 

##### MovimientoCaja

##### 

##### EstadoSesionCaja

##### 

##### TipoMovimientoCaja

##### 

##### MetodoPago en Shared/Enums.

##### 

##### \### MetodoPago

##### 

##### Efectivo = 1

##### 

##### Yape = 2

##### 

##### Plin = 3

##### 

##### \### EstadoSesionCaja

##### 

##### Abierta = 1

##### 

##### Cerrada = 2

##### 

##### \### TipoMovimientoCaja

##### 

##### IngresoVenta = 1

##### 

##### EgresoAbastecimiento = 2

##### 

##### ReversionVenta = 3

##### 

##### Consultar docs/modelo-datos.md para los campos de las entidades.

##### 

##### \## Application

##### 

##### Crear:

##### 

##### ICajaRepository

##### 

##### AbrirCajaUseCase

##### 

##### CerrarCajaUseCase

##### 

##### \## Apertura

##### 

##### No permitir más de una caja abierta.

##### 

##### FondoInicial debe ser >= 0.

##### 

##### Registrar fecha y usuario de apertura.

##### 

##### Estado inicial = Abierta.

##### 

##### \## Cierre

##### 

##### Obtener sesión abierta.

##### 

##### Calcular esperado por medio de pago.

##### 

##### Recibir valores reales.

##### 

##### Calcular diferencias.

##### 

##### Registrar usuario y fecha de cierre.

##### 

##### Estado final = Cerrada.

##### 

##### \## Infrastructure

##### 

##### Crear:

##### 

##### CajaRepository

##### 

##### SesionCajaConfiguration

##### 

##### MovimientoCajaConfiguration

##### 

##### No generar migración.

##### 

##### \## Desktop

##### 

##### Crear módulo Caja con View y ViewModel básicos.

##### 

##### No modificar navegación global.

##### 

##### \## Tests mínimos

##### 

##### \- caja válida puede abrirse,

##### \- no pueden existir dos cajas abiertas,

##### \- fondo negativo falla,

##### \- una caja abierta puede cerrarse,

##### \- diferencias se calculan correctamente,

##### \- cerrar sin caja abierta falla.

##### 

##### \---

##### 

##### \# Uso de IA

##### 

##### Antes de modificar código, leer:

##### 

##### AGENTS.md

##### 

##### docs/arquitectura.md

##### 

##### docs/convenciones.md

##### 

##### docs/modelo-datos.md

##### 

##### docs/sprints/sprint-01.md

##### 

##### La IA debe mostrar primero qué archivos planea modificar.

##### 

##### No solicitar implementar todo el módulo de una sola vez.

##### 

##### Trabajar progresivamente:

##### 

##### Domain

##### → Application

##### → Infrastructure

##### → Tests

##### → Desktop

##### 

##### No realizar commits, push, PR ni merge automáticamente.

##### 

##### \## Criterio general de terminado

##### 

##### El módulo debe:

##### 

##### compilar,

##### 

##### tener tests relacionados exitosos,

##### 

##### respetar la arquitectura,

##### 

##### no generar migraciones,

##### 

##### no modificar archivos compartidos sin coordinación,

##### 

##### y estar listo para Pull Request hacia develop.

