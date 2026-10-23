# Predicción de demanda v2 · pronóstico semanal

Reemplaza el horizonte de tres días de la v1 (`prediccion-demanda-v1.md`) por un ciclo semanal.
Se mantienen el modelo SSA de ML.NET, la separación Mi negocio / Ver un ejemplo, la confirmación
de historial completo y el carácter referencial de las cifras. No cambia el esquema de datos ni
requiere migración. No se modificaron archivos centrales compartidos (App.xaml.cs,
DependencyInjection.cs, MainWindowViewModel.cs, ViewTemplates.xaml).

## Flujo

1. **Semana analizada.** La administradora elige una de las últimas 8 semanas ya terminadas
   (lunes a domingo). Por defecto, la semana pasada. La semana en curso no se puede elegir.
2. **Ventas de la semana.** El sistema determina, por producto, la cantidad vendida en esa semana
   (sin ventas anuladas). Solo continúan los productos que **sí se vendieron**; los demás se
   cuentan como "sin ventas en la semana" y no se estiman.
3. **Estimación de la semana siguiente.** Para cada producto vendido, SSA se entrena con la serie
   diaria hasta el domingo analizado y pronostica 7 días; la estimación es la suma de esos días.
   Nunca se usan ventas posteriores al domingo analizado para entrenar.
4. **Comparación.**
   - Cambio porcentual frente a lo vendido en la semana analizada (Similar si es menor a ±5 %).
   - Si la semana estimada todavía no termina: riesgo de agotamiento con el stock actual
     (Alto: stock ≤ estimación; Medio: stock ≤ 1,5 × estimación; Bajo: resto).
   - Si la semana estimada ya terminó (se eligió una semana antigua): se muestra lo vendido
     realmente para contrastar la estimación, y no se calcula riesgo con el stock actual.

Se incluyen también productos activos que no controlan inventario: se estiman sus ventas, pero
se muestran como "Sin control de stock" y sin riesgo.

## Política de datos

| Regla | Valor |
|---|---|
| Horizonte | 7 días (semana siguiente) |
| Mínimo de historial hasta el domingo analizado | 21 días (14 para entrenar + 1 semana para comprobar) |
| Etiqueta Experimental | menos de 56 días (8 semanas) |
| Días con ventas exigidos | 7 (experimental) / 14 (referencial) |
| Semanas de comprobación | 1 a 4 bloques de 7 días, según el historial |
| Referencia para comparar el error | repetir lo vendido la semana previa |
| Ventana SSA | 3 con menos de 28 días; 7 (ciclo semanal) desde 28 días, longitud 28 |

Si el producto no tiene historial suficiente, se informa lo vendido en la semana pero **no se
genera estimación** (requerimiento del componente de BI y analítica predictiva del informe).

## Ejemplo

Ocho semanas sintéticas en memoria para leche, agua y papa; un producto nuevo con 10 días
(Datos insuficientes) y una gaseosa sin ventas en la semana analizada (no aparece).

## Pruebas

`tests/BodegaLuchito.Tests/Application/BI/PrediccionDemandaTests.cs` cubre semanas lunes-domingo,
exclusión de productos sin ventas, bloques de evaluación de 7 días sin fuga de datos futuros,
comparación con la venta real en semanas antiguas, límites de historial y SSA real en 21, 27,
28, 56 y 70 días.
