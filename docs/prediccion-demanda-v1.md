# Predicción de demanda v1

Implementada sobre `develop` en `37fcbbdb49f2154b35826785e067d705dbb95b56`.

## Alcance

La administradora puede abrir **Predicción de demanda** desde el menú. El módulo estima
cantidades vendidas en los próximos tres días calendario y las compara con el stock
consultado. Incluye productos activos que controlan inventario, tanto por unidad como por kg.
Es una estimación referencial, no una orden de compra ni una probabilidad de agotamiento.
No modifica productos, movimientos, ventas, caja ni el esquema de datos. No necesita migración.
Los reportes descriptivos y el dashboard quedan fuera de este cambio.

## Dos orígenes separados

**Datos del negocio:** consulta SQLite mediante EF Core y el contrato de Application.
Seleccionar la fecha desde la cual se registraron todas las ventas; máximo 180 días atrás.
La fecha efectiva por producto también respeta su alta y la primera venta válida del negocio.
Se excluyen el día de alta del producto y el día actual para evitar períodos parciales.
Las ventas anuladas no participan. Se suman cantidades, no importes ni movimientos manuales.
Las cantidades en kg mantienen sus decimales.

Antes de estimar, se exige confirmar que el período está completo. Solo bajo esa declaración
los días sin registros del producto se interpretan como cero. El sistema no puede detectar
ventas no registradas ni distinguir automáticamente registros reales de pruebas. No confirmar
para obtener cifras si falta información. Los días sin stock pueden ocultar demanda no atendida.
Si una unidad de venta cambió durante el período, elegir un período posterior al cambio:
esta versión no reconstruye cambios históricos de unidad.

**Demostración:** genera en memoria 15 días sintéticos reproducibles para leche, agua y papa,
y ocho días para un producto nuevo que demuestra el estado de datos insuficientes. No consulta
ni escribe la base real, ni usa su stock. Los nombres contienen “ejemplo” y la pantalla señala
el modo. Evaluar estos datos demuestra el funcionamiento técnico, no la precisión comercial.
No hay selector de cantidad de días ni de etapas. Basta elegir Demostración y analizar.
Piloto y uso real emplean el mismo origen Datos del negocio: si contiene pruebas, sus resultados
son de prueba. Cuando se instale, usar una base de producción separada de las pruebas.

## Modelo y evaluación

Se agrega `Microsoft.ML.TimeSeries` **5.0.0**, versión estable, exclusivamente en Infrastructure.
Proporciona SSA y trae ML.NET y bibliotecas nativas como dependencias. La primera restauración
requiere internet; el entrenamiento y la predicción posteriores son locales y no requieren claves
ni servicios externos. Se verificó en Windows x64 / .NET 10. Incluir dependencias nativas al distribuir
la aplicación; otras arquitecturas requieren validación aparte.

- SSA univariado: horizonte 3 y semilla 42. Con menos de 30 observaciones de entrenamiento,
  ventana 3 y longitud de serie igual al menor entre 14 y las observaciones disponibles.
  A partir de 30 observaciones de entrenamiento, ventana 7 y longitud de serie 30.
  La ventana corta es una configuración experimental, no una afirmación de estacionalidad semanal.
- Política inicial: mínimo 15 días completos. Entre 15 y 59 días se exige al menos 7 días con ventas
  y se etiqueta **Experimental**. Desde 60 días se exigen al menos 14 días con ventas y la etiqueta
  es **Estimación referencial**, sin garantizar precisión por alcanzar esa antigüedad.
  Es un criterio configurable en código para esta versión, no una garantía estadística ni un
  mínimo universal de ML.NET. Las series constantes o muy escasas se omiten.
- Evaluación temporal: se reservan entre uno y cuatro bloques de tres días, manteniendo al menos
  12 días para el primer entrenamiento. Con 15 días, se entrena con los primeros 12 y se comprueba
  el total de los últimos tres: **una sola evaluación**, insuficiente para medir estabilidad.
  Con 18 días hay dos bloques; con 21 hay tres; desde 24 hay cuatro. Son decisiones internas,
  no opciones que deba configurar la dueña. El primer entrenamiento también debe tener al menos
  siete días con ventas y cantidades variables; si no, se muestra Datos insuficientes. Cada bloque
  se predice entrenando solo con días anteriores. Nunca se usa el bloque futuro para entrenarlo.
- Comparación: promedio de los últimos siete días del mismo entrenamiento multiplicado por tres.
- Se muestra el error absoluto medio de los **totales de tres días**, en unid o kg. Menor es mejor.
  No se presenta como porcentaje de precisión ni se mezclan errores entre productos o unidades.
- Después se entrena con todo el período disponible y se estiman hoy y los próximos dos días.
  El resultado se muestra hasta tres decimales; una estimación en unidades puede ser fraccionaria.
- Las salidas negativas de SSA se limitan a cero; los resultados no finitos se rechazan.
- Si el modelo no supera al promedio, se informa expresamente; no se cambia silenciosamente
  de algoritmo ni se asegura que el pronóstico sea confiable.
- Cada análisis reentrena en memoria. No guarda modelos ni pronósticos históricos. La ejecución
  ocurre fuera del hilo de interfaz y permite cancelar entre entrenamientos; un entrenamiento
  nativo que ya comenzó termina antes de atender la cancelación.

Fuentes técnicas:
- [Pronóstico de demanda con ML.NET](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/time-series-demand-forecasting)
- [Paquete oficial Microsoft.ML.TimeSeries](https://www.nuget.org/packages/Microsoft.ML.TimeSeries/5.0.0)

## Riesgo (regla posterior al modelo)

| Nivel | Regla |
|---|---|
| Alto | Stock agotado o stock menor o igual a la demanda estimada de tres días. |
| Medio | Stock mayor a la demanda, pero menor o igual a 1.5 veces esa demanda. |
| Bajo | Stock mayor a 1.5 veces la demanda. |
| Sin clasificación | No hay predicción; se muestra el motivo. |

Estos umbrales son una política inicial explícita, no una clasificación aprendida. No usan
plazos del proveedor ni entregas pendientes. El stock corresponde al instante de consulta;
volver a analizar después de ventas, abastecimientos o ajustes.

## Interfaz y comprobación manual

La pantalla usa la paleta azul, texto oscuro al pasar el mouse o seleccionar filas y una tabla
con desplazamiento propio. En ventanas muy pequeñas hay desplazamiento exterior de respaldo
para conservar accesibles los controles y la evaluación. No se ocultan columnas: cuando no caben,
la tabla permite desplazamiento horizontal. Los resultados se limpian al cambiar origen, fecha
o confirmación para no confundir resultados anteriores con la selección actual.

1. Iniciar sesión como administradora y abrir Predicción de demanda.
2. Analizar Datos del negocio con los registros recientes: debe indicar Datos insuficientes.
3. Cambiar a Demostración y pulsar Analizar demanda, sin seleccionar cantidades de días: deben
   aparecer tres estimaciones experimentales y un producto sin historial suficiente. Seleccionar
   cada fila para ver sus errores, el número de períodos evaluados y la limitación del historial corto.
4. Comprobar que stock, ventas, caja y movimientos reales no cambiaron.
5. Volver a Datos del negocio: desaparecen inmediatamente las cifras simuladas.
6. Probar fecha vacía/futura, cancelar un cálculo y navegar a otro módulo durante el análisis.
7. Probar selección, rueda del mouse, tamaño de laptop y ventana pequeña.
8. Con un conjunto completo de al menos 15 días, comprobar exclusión de ventas anuladas,
   conservación de kg y tratamiento de días sin ventas. No insertar ventas ficticias en producción
   para desbloquear el modelo: usar Demostración.
9. Para el piloto, el primer día de alta del producto no se cuenta porque puede ser parcial.
   Quince días completos no equivalen a quince comprobantes ni a registrar todas las pruebas hoy.

## Archivos

- `Application/BI`: contratos, política de análisis, evaluación temporal y generador de demostración.
- `Infrastructure/BI`: lectura agregada de ventas y adaptador ML.NET SSA.
- `Desktop/Modules/BI`: ViewModel y vista de predicción.
- Integración: `App.xaml.cs`, `Resources/ViewTemplates.xaml`, `MainWindowViewModel.cs`,
  `Infrastructure/DependencyInjection.cs` y referencia de paquete en Infrastructure.
- `tests/BodegaLuchito.Tests/Application/BI`: pruebas de modelo, política y repositorio SQLite.

## Validación automática

Ejecutar `dotnet build` y `dotnet test`. Las pruebas cubren SSA real sobre la demostración,
separación de la base real, ausencia de fuga temporal, comparación del error, ceros confirmados,
cantidades por peso, datos insuficientes, acceso, cancelación y lectura de SQLite sin escrituras.
También cubren el límite de 14/15 días, la reserva de días futuros en el piloto y la ejecución
de SSA con historiales cortos y con la configuración de historial más amplio.

La validación de calidad con ventas reales permanece pendiente hasta disponer de un historial
confiable. Para sustentar ante el docente, mostrar datos de entrenamiento, períodos reservados,
errores frente al promedio y limitaciones; usar ML.NET por sí solo no acredita calidad predictiva.
