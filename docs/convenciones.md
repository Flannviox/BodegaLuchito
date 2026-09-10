##### \# Convenciones de desarrollo

##### 

##### \## Tecnologías

##### 

##### \- C# 14

##### \- .NET 10

##### \- WPF

##### \- MVVM

##### \- Entity Framework Core

##### \- SQLite

##### \- xUnit

##### 

##### \## Idioma del código

##### 

##### Los conceptos propios del negocio se escribirán en español.

##### 

##### Ejemplos:

##### 

##### Producto

##### Venta

##### Proveedor

##### RegistrarVenta

##### CierreCaja

##### 

##### No utilizar tildes ni caracteres especiales en nombres de clases,

##### métodos, propiedades, archivos o carpetas.

##### 

##### \## Nombres

##### 

##### Clases, métodos y propiedades:

##### PascalCase

##### 

##### Variables locales y parámetros:

##### camelCase

##### 

##### Ejemplo:

##### 

##### Ejemplo:

##### 

##### ```csharp

##### public class Producto

##### {

##### &#x20;   public string Nombre { get; set; } = string.Empty;

##### }

##### ```

##### \## Async

##### 

##### Los métodos asincrónicos deben terminar con Async.

##### 

##### Ejemplo:

##### 

##### GuardarProductoAsync()

##### 

##### \## Responsabilidades

##### 

##### No colocar lógica de negocio en Views ni en code-behind de WPF.

##### 

##### Domain contiene entidades, enums, value objects y reglas fundamentales

##### del negocio.

##### 

##### Application contiene casos de uso, DTOs, interfaces y contratos entre

##### módulos.

##### 

##### Infrastructure contiene Entity Framework Core, repositorios,

##### persistencia e integraciones externas.

##### 

##### Desktop contiene Views, ViewModels, navegación y composición de UI.

##### 

##### Desktop no debe acceder directamente a SQLite.

##### 

##### Un módulo no debe modificar directamente la persistencia interna

##### de otro módulo.

##### \## Módulos

##### 

##### Cada funcionalidad debe colocarse dentro de su módulo correspondiente.

##### 

##### Ejemplo:

##### 

##### Domain/Productos

##### Application/Productos

##### Infrastructure/Productos

##### Desktop/Modules/Productos

##### 

##### \## Código

##### 

##### Evitar clases gigantes.

##### 

##### Evitar duplicación de código.

##### 

##### Mantener Nullable habilitado.

##### 

##### No utilizar valores mágicos cuando corresponda una constante,

##### enumeración o configuración.

##### 

##### No introducir una nueva dependencia NuGet sin justificar su necesidad.

##### 

##### \## Entity Framework Core

##### 

##### Las entidades de Domain no deben utilizar atributos específicos

##### de Entity Framework Core.

##### 

##### La configuración de persistencia debe realizarse mediante Fluent API

##### dentro de Infrastructure.

##### 

##### Las migraciones deben mantenerse dentro de:

##### 

##### Infrastructure/Persistence/Migrations

