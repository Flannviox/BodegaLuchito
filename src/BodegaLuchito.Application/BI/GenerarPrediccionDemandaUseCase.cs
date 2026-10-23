using BodegaLuchito.Application.Common.Session;

namespace BodegaLuchito.Application.BI;

/// <summary>
/// Pronóstico semanal de ventas. Toma la semana (lunes a domingo) que acaba de terminar, determina
/// cuánto se vendió de cada producto y, solo para los productos que sí se vendieron, estima con el
/// modelo cuánto se vendería en la semana siguiente. Si la semana estimada ya pasó, se informa lo
/// vendido realmente para comparar. Las cifras son referenciales: no realizan compras ni mueven stock.
/// </summary>
public sealed class GenerarPrediccionDemandaUseCase(
    IHistorialDemandaRepository historial, IModeloDemanda modelo, ISesionUsuario sesion)
{
    /// <summary>Días estimados: una semana completa.</summary>
    public const int Horizonte = 7;
    public const int MinimoDiasEntrenamiento = 14;
    /// <summary>Dos semanas para entrenar y una para comprobar el modelo antes de usarlo.</summary>
    public const int MinimoDias = MinimoDiasEntrenamiento + Horizonte;
    /// <summary>Con menos de ocho semanas la estimación se marca como experimental.</summary>
    public const int ReferenciaHistorialAmplio = 56;
    public const int MaximoDias = 180;
    public const int SemanasConsultables = 8;
    private const int BloquesEvaluacion = 4;

    /// <summary>Lunes de la semana que contiene la fecha.</summary>
    public static DateTime InicioSemana(DateTime fecha) =>
        fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    /// <summary>Lunes de la última semana completa (la que terminó el domingo pasado).</summary>
    public static DateTime UltimaSemanaCerrada(DateTime hoy) => InicioSemana(hoy).AddDays(-7);

    /// <summary>Lunes de las semanas cerradas que se pueden consultar, de la más reciente a la más antigua.</summary>
    public static IReadOnlyList<DateTime> SemanasCerradas(DateTime hoy) =>
        Enumerable.Range(0, SemanasConsultables).Select(i => UltimaSemanaCerrada(hoy).AddDays(-7 * i)).ToArray();

    public async Task<InformePrediccion> EjecutarAsync(bool demostracion, DateTime semana, DateTime desde,
        bool historialCompleto, CancellationToken cancellationToken = default)
    {
        if (sesion.UsuarioActual?.EsAdministradora != true)
            throw new InvalidOperationException("Solo la administradora puede consultar predicciones.");

        var ahora = DateTime.Now;
        var hoy = ahora.Date;
        var ultimaCerrada = UltimaSemanaCerrada(hoy);
        var inicio = demostracion ? ultimaCerrada : InicioSemana(semana);
        if (inicio > ultimaCerrada || inicio < ultimaCerrada.AddDays(-7 * (SemanasConsultables - 1)))
            throw new ArgumentException($"Selecciona una de las últimas {SemanasConsultables} semanas que ya terminaron (de lunes a domingo).");

        var corte = inicio.AddDays(7);                 // Lunes siguiente: fin exclusivo de la semana analizada.
        var finProyeccion = corte.AddDays(Horizonte);  // Fin exclusivo de la semana estimada.
        var proyeccionTerminada = finProyeccion <= hoy;

        if (demostracion) desde = corte.AddDays(-DatosDemostracionDemanda.Dias);
        else if (desde.Date > inicio || desde.Date < hoy.AddDays(-MaximoDias))
            throw new ArgumentException($"La fecha desde la que registras ventas debe ser anterior al inicio de la semana elegida ({inicio:dd/MM/yyyy}) y no mayor a {MaximoDias} días atrás.");

        var datos = demostracion ? DatosDemostracionDemanda.Crear(corte)
            : await historial.ConsultarAsync(desde.Date, proyeccionTerminada ? finProyeccion : hoy, cancellationToken);

        var periodo = new Periodo(inicio, corte, finProyeccion, proyeccionTerminada);
        // El entrenamiento es CPU intensivo: se ejecuta fuera del hilo de WPF, secuencialmente.
        var resultados = await Task.Run(() => datos.Select(producto =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Analizar(producto, periodo, demostracion || historialCompleto, cancellationToken);
        }).OfType<ResultadoPrediccion>()
          .OrderBy(x => x.Demanda.HasValue ? x.Riesgo == "Alto" ? 0 : x.Riesgo == "Medio" ? 1 : 2 : 3)
          .ThenByDescending(x => x.VendidoSemana).ThenBy(x => x.Producto).ToArray(), cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return new(demostracion, ahora, inicio, corte.AddDays(-1), corte, finProyeccion.AddDays(-1),
            proyeccionTerminada, datos.Count - resultados.Length, resultados);
    }

    private sealed record Periodo(DateTime Inicio, DateTime Corte, DateTime FinProyeccion, bool ProyeccionTerminada);

    /// <returns>null cuando el producto no tuvo ventas en la semana analizada.</returns>
    private ResultadoPrediccion? Analizar(HistorialDemanda producto, Periodo periodo, bool completo,
        CancellationToken cancellationToken)
    {
        decimal Sumar(DateTime desdeInclusivo, DateTime hastaExclusiva) => producto.Ventas
            .Where(v => v.Fecha >= desdeInclusivo && v.Fecha < hastaExclusiva).Sum(v => v.Cantidad);

        // Paso 1: determinar lo vendido en la semana que terminó. Solo siguen los productos vendidos.
        var vendido = Sumar(periodo.Inicio, periodo.Corte);
        if (vendido <= 0) return null;

        var desdeProducto = producto.Desde.Date;
        var dias = Math.Max(0, (periodo.Corte - desdeProducto).Days);
        decimal? anterior = desdeProducto <= periodo.Inicio.AddDays(-7)
            ? Sumar(periodo.Inicio.AddDays(-7), periodo.Inicio) : null;
        decimal? real = periodo.ProyeccionTerminada ? Sumar(periodo.Corte, periodo.FinProyeccion) : null;
        // El riesgo compara el stock actual con la semana que viene; no aplica a semanas ya pasadas.
        var evaluaStock = producto.ControlaInventario && !periodo.ProyeccionTerminada;

        ResultadoPrediccion SinPrediccion(string estado, string detalle) =>
            new(producto.ProductoId, producto.Producto, producto.Unidad, producto.Stock, producto.ControlaInventario,
                dias, estado, vendido, anterior, null, real, "—", null, null, detalle);

        if (dias < MinimoDias)
            return SinPrediccion("Datos insuficientes", $"Hay {dias} días completos hasta el fin de la semana; se requieren al menos {MinimoDias} (tres semanas).");
        if (!completo)
            return SinPrediccion("Confirmar historial", "Confirma que el período contiene todas las ventas y que los días sin registros realmente no tuvieron ventas.");

        // Paso 2: serie diaria hasta el domingo de la semana analizada. Nunca se usan días posteriores.
        var porDia = producto.Ventas.Where(x => x.Fecha >= desdeProducto && x.Fecha < periodo.Corte)
            .GroupBy(x => x.Fecha.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        if (porDia.Values.Any(x => x < 0))
            return SinPrediccion("Revisar datos", "El historial contiene cantidades negativas.");
        var serie = Enumerable.Range(0, dias)
            .Select(i => porDia.GetValueOrDefault(desdeProducto.AddDays(i))).ToArray();
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
            decimal errorModelo = 0, errorSemanaAnterior = 0;
            // Paso 3: comprobar el modelo con hasta cuatro semanas anteriores, sin entrenar con la semana evaluada.
            for (var corte = primerCorte; corte < dias; corte += Horizonte)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entrenamiento = serie[..corte];
                var esperado = serie.Skip(corte).Take(Horizonte).Sum();
                errorModelo += Math.Abs(ValidarPronostico(modelo.Predecir(entrenamiento, Horizonte)).Sum() - esperado);
                // Referencia simple: suponer que se venderá lo mismo que la semana previa.
                errorSemanaAnterior += Math.Abs(entrenamiento.TakeLast(Horizonte).Sum() - esperado);
            }
            // Paso 4: estimar la semana siguiente con todo el historial hasta el domingo analizado.
            var demanda = decimal.Round(ValidarPronostico(modelo.Predecir(serie, Horizonte)).Sum(), 3);
            cancellationToken.ThrowIfCancellationRequested();
            var mae = decimal.Round(errorModelo / bloques, 3);
            var maeSemana = decimal.Round(errorSemanaAnterior / bloques, 3);
            var detalle = mae < maeSemana
                ? "El modelo tuvo menor error que repetir lo vendido la semana previa en la evaluación reciente."
                : "El modelo no superó a repetir lo vendido la semana previa; interpreta esta estimación con cautela.";
            if (experimental)
                detalle = $"Historial corto ({dias} días): estimación experimental. " + detalle;
            if (bloques == 1)
                detalle += " Solo se comprobó una semana; aún no permite evaluar la estabilidad del modelo.";
            return new(producto.ProductoId, producto.Producto, producto.Unidad, producto.Stock,
                producto.ControlaInventario, dias, experimental ? "Experimental" : "Estimación referencial",
                vendido, anterior, demanda, real,
                evaluaStock ? ClasificarRiesgo(producto.Stock, demanda) : "—", mae, maeSemana, detalle, bloques);
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
