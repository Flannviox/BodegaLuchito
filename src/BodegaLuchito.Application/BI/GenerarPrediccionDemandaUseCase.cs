using BodegaLuchito.Application.Common.Session;

namespace BodegaLuchito.Application.BI;

public sealed class GenerarPrediccionDemandaUseCase(
    IHistorialDemandaRepository historial, IModeloDemanda modelo, ISesionUsuario sesion)
{
    public const int Horizonte = 3;
    public const int MinimoDias = 15;
    public const int MinimoDiasEntrenamiento = 12;
    public const int ReferenciaHistorialAmplio = 60;
    public const int MaximoDias = 180;
    private const int BloquesEvaluacion = 4;

    public async Task<InformePrediccion> EjecutarAsync(bool demostracion, DateTime desde,
        bool historialCompleto, CancellationToken cancellationToken = default)
    {
        if (sesion.UsuarioActual?.EsAdministradora != true)
            throw new InvalidOperationException("Solo la administradora puede consultar predicciones.");

        var ahora = DateTime.Now;
        var hoy = ahora.Date;
        if (demostracion) desde = hoy.AddDays(-DatosDemostracionDemanda.Dias);
        if (desde.Date >= hoy || desde.Date < hoy.AddDays(-MaximoDias))
            throw new ArgumentException($"Selecciona una fecha entre ayer y hace {MaximoDias} días.");

        var datos = demostracion ? DatosDemostracionDemanda.Crear(hoy)
            : await historial.ConsultarAsync(desde.Date, hoy, cancellationToken);

        // El entrenamiento es CPU intensivo: se ejecuta fuera del hilo de WPF, secuencialmente.
        var resultados = await Task.Run(() => datos.Select(producto =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Analizar(producto, hoy, demostracion || historialCompleto, cancellationToken);
        }).OrderBy(x => x.Demanda.HasValue ? x.Riesgo == "Alto" ? 0 : x.Riesgo == "Medio" ? 1 : 2 : 3)
          .ThenBy(x => x.Producto).ToArray(), cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return new(demostracion, ahora, desde.Date, hoy.AddDays(-1), resultados);
    }

    private ResultadoPrediccion Analizar(HistorialDemanda producto, DateTime hoy, bool completo,
        CancellationToken cancellationToken)
    {
        var dias = Math.Max(0, (hoy - producto.Desde.Date).Days);
        ResultadoPrediccion SinPrediccion(string estado, string detalle) =>
            new(producto.ProductoId, producto.Producto, producto.Unidad, producto.Stock, dias,
                estado, null, "—", null, null, detalle);

        if (dias < MinimoDias)
            return SinPrediccion("Datos insuficientes", $"Hay {dias} días completos; esta versión requiere al menos {MinimoDias}.");
        if (!completo)
            return SinPrediccion("Confirmar historial", "Confirma que el período contiene todas las ventas y que los días sin registros realmente no tuvieron ventas.");

        var porDia = producto.Ventas.Where(x => x.Fecha >= producto.Desde.Date && x.Fecha < hoy)
            .GroupBy(x => x.Fecha.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        if (porDia.Values.Any(x => x < 0))
            return SinPrediccion("Revisar datos", "El historial contiene cantidades negativas.");
        var serie = Enumerable.Range(0, dias)
            .Select(i => porDia.GetValueOrDefault(producto.Desde.Date.AddDays(i))).ToArray();
        var experimental = dias < ReferenciaHistorialAmplio;
        var minimoDiasConVentas = experimental ? 7 : 14;
        if (serie.Count(x => x > 0) < minimoDiasConVentas || serie.Distinct().Count() < 2)
            return SinPrediccion("Datos insuficientes", $"Se requieren al menos {minimoDiasConVentas} días con ventas y variación en las cantidades para evaluar este modelo.");

        var bloques = Math.Min(BloquesEvaluacion, (dias - MinimoDiasEntrenamiento) / Horizonte);
        var primerCorte = dias - bloques * Horizonte;
        // Un producto que recién empieza a venderse no tiene un pasado suficiente para evaluar.
        var primeraSerie = serie[..primerCorte];
        if (primeraSerie.Count(x => x > 0) < 7 || primeraSerie.Distinct().Count() < 2)
            return SinPrediccion("Datos insuficientes", "Faltan ventas previas para evaluar: el primer entrenamiento necesita al menos 7 días con ventas y cantidades variables.");

        try
        {
            decimal errorModelo = 0, errorPromedio = 0;
            // De uno a cuatro bloques de tres días, sin entrenar con los días que se evalúan.
            for (var corte = primerCorte; corte < dias; corte += Horizonte)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entrenamiento = serie[..corte];
                var esperado = serie.Skip(corte).Take(Horizonte).Sum();
                errorModelo += Math.Abs(ValidarPronostico(modelo.Predecir(entrenamiento, Horizonte)).Sum() - esperado);
                errorPromedio += Math.Abs(entrenamiento.TakeLast(7).Average() * Horizonte - esperado);
            }
            var demanda = decimal.Round(ValidarPronostico(modelo.Predecir(serie, Horizonte)).Sum(), 3);
            cancellationToken.ThrowIfCancellationRequested();
            var mae = decimal.Round(errorModelo / bloques, 3);
            var maePromedio = decimal.Round(errorPromedio / bloques, 3);
            var detalle = mae < maePromedio
                ? "El modelo tuvo menor error que el promedio en la evaluación reciente."
                : "El modelo no superó al promedio reciente; interpreta esta estimación con cautela.";
            if (experimental)
                detalle = $"Historial corto ({dias} días): estimación experimental. " + detalle;
            if (bloques == 1)
                detalle += " Solo se comprobó un período de 3 días; aún no permite evaluar la estabilidad del modelo.";
            return new(producto.ProductoId, producto.Producto, producto.Unidad, producto.Stock, dias,
                experimental ? "Experimental" : "Estimación referencial", demanda,
                ClasificarRiesgo(producto.Stock, demanda), mae, maePromedio, detalle, bloques);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ArithmeticException)
        {
            System.Diagnostics.Trace.TraceWarning($"Predicción producto {producto.ProductoId}: {ex}");
            return SinPrediccion("No se pudo estimar", "El modelo no pudo evaluar esta serie. Revisa el historial o vuelve a calcular.");
        }
    }

    private static decimal[] ValidarPronostico(decimal[] valores)
    {
        if (valores.Length != Horizonte || valores.Any(x => x < 0))
            throw new InvalidOperationException("Pronóstico inválido.");
        return valores;
    }

    public static string ClasificarRiesgo(decimal stock, decimal demanda) =>
        stock <= 0 || stock <= demanda ? "Alto" : stock <= demanda * 1.5m ? "Medio" : "Bajo";
}
