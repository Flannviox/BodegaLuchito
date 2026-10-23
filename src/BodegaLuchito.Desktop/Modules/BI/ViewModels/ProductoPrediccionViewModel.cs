using System.Globalization;
using BodegaLuchito.Application.BI;

namespace BodegaLuchito.Desktop.Modules.BI.ViewModels;

/// <summary>
/// Tarjeta de un producto pensada para la dueña o un vendedor sin conocimientos técnicos:
/// cifras redondeadas, un color tipo semáforo y un consejo en lenguaje cotidiano.
/// Conserva el resultado original para los detalles técnicos.
/// </summary>
public sealed class ProductoPrediccionViewModel(ResultadoPrediccion resultado, bool proyeccionTerminada)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");

    // Glifos de Segoe MDL2 Assets / Segoe Fluent Icons (incluidas en Windows 10 y 11).
    private const string IconoAlerta = "";
    private const string IconoReloj = "";
    private const string IconoListo = "";
    private const string IconoInfo = "";

    public ResultadoPrediccion Resultado { get; } = resultado;
    public bool ProyeccionTerminada { get; } = proyeccionTerminada;
    public string Producto => Resultado.Producto;
    public string Riesgo => Resultado.Riesgo;
    public bool TieneEstimacion => Resultado.Demanda is not null;
    public bool SinEstimacion => !TieneEstimacion;
    public bool MuestraStock => Resultado.ControlaInventario && !ProyeccionTerminada;
    public bool MuestraVentaReal => TieneEstimacion && Resultado.VentaReal is not null;

    // ---- Cifras grandes de la tarjeta ----
    public string Vendido => Cantidad(Resultado.VendidoSemana);
    public string Estimado => Resultado.Demanda is decimal demanda ? $"≈ {Cantidad(demanda, estimada: true)}" : "—";
    public string Existencias => Cantidad(Resultado.Stock);
    public string VentaReal => Resultado.VentaReal is decimal real ? Cantidad(real) : "—";

    /// <summary>Variación porcentual de la estimación respecto a lo vendido en la semana revisada.</summary>
    public decimal? Variacion => Resultado.Demanda is decimal demanda && Resultado.VendidoSemana > 0
        ? decimal.Round((demanda - Resultado.VendidoSemana) / Resultado.VendidoSemana * 100, 1) : null;
    public string Tendencia => Variacion switch
    {
        null => "",
        > -5 and < 5 => "= Casi lo mismo",
        > 0 => $"▲ {Variacion.Value.ToString("0", Cultura)} % más",
        _ => $"▼ {Math.Abs(Variacion.Value).ToString("0", Cultura)} % menos"
    };
    public bool TieneTendencia => !string.IsNullOrEmpty(Tendencia);

    // ---- Semáforo: Alto (rojo), Medio (ámbar), Bajo (verde), Neutro (azul gris) ----
    public string Nivel => Resultado.Demanda is not decimal demanda ? "Neutro"
        : ProyeccionTerminada ? NivelAcierto(demanda)
        : !Resultado.ControlaInventario ? "Neutro"
        : Resultado.Stock <= 0 ? "Alto"
        : Riesgo is "Alto" or "Medio" or "Bajo" ? Riesgo : "Neutro";

    public string Etiqueta => Resultado.Demanda is null ? "Aún sin cálculo"
        : ProyeccionTerminada ? Nivel switch { "Bajo" => "Acertó", "Medio" => "Estuvo cerca", _ => "Se alejó" }
        : !Resultado.ControlaInventario ? "Sin control de stock"
        : Resultado.Stock <= 0 ? "Sin stock"
        : Nivel switch { "Alto" => "Podría faltar", "Medio" => "Queda poco", "Bajo" => "Te alcanza", _ => "Revisar" };

    public string Icono => Nivel switch
    {
        "Alto" => IconoAlerta,
        "Medio" => ProyeccionTerminada ? IconoInfo : IconoReloj,
        "Bajo" => IconoListo,
        _ => IconoInfo
    };

    /// <summary>Qué hacer, en una o dos frases cortas.</summary>
    public string Consejo
    {
        get
        {
            if (Resultado.Demanda is not decimal demanda) return SinCalculo();
            if (ProyeccionTerminada && Resultado.VentaReal is decimal real)
            {
                var diferencia = Math.Abs(real - (Resultado.Unidad == "kg" ? demanda : decimal.Ceiling(demanda)));
                return diferencia == 0
                    ? "El cálculo coincidió con lo que realmente se vendió."
                    : $"En realidad se vendieron {Cantidad(real)}. El cálculo se diferenció en {Cantidad(diferencia)}.";
            }
            if (!Resultado.ControlaInventario)
                return "Este producto no lleva control de stock. Usa la cifra para saber cuánto comprar.";
            if (Resultado.Stock <= 0)
                return "Ya no te queda. Compra antes de que te lo pidan los clientes.";
            return Riesgo switch
            {
                "Alto" => $"Compra más pronto: con {Existencias} no alcanzaría para la próxima semana.",
                "Medio" => "Te alcanzaría muy justo. Si puedes, compra un poco más.",
                _ => "Tienes suficiente para la próxima semana. Por ahora no necesitas comprar."
            };
        }
    }

    /// <summary>Recordatorio breve sobre la confianza del cálculo.</summary>
    public string Nota
    {
        get
        {
            if (Resultado.Demanda is null) return "";
            if (Resultado.Estado == "Experimental")
                return "Calculado con pocas semanas de ventas. Tómalo solo como referencia.";
            return Resultado.ErrorModelo >= Resultado.ErrorSemanaAnterior
                ? "Es un aproximado. Últimamente este producto ha sido difícil de calcular."
                : "Es un aproximado: las ventas reales pueden variar un poco.";
        }
    }
    public bool TieneNota => !string.IsNullOrEmpty(Nota);

    /// <summary>Dato de apoyo de la lista: stock actual o, si la semana ya pasó, lo vendido en realidad.</summary>
    public string Apoyo => ProyeccionTerminada ? VentaReal
        : Resultado.ControlaInventario ? Existencias : "No se controla";

    /// <summary>Confianza en palabras: compara el error de las semanas anteriores con lo que se vende en una semana.</summary>
    public string Confianza
    {
        get
        {
            if (Resultado.ErrorModelo is not decimal error) return "";
            var proporcion = error / Math.Max(Resultado.VendidoSemana, 1);
            var nivel = proporcion <= 0.2m ? "alta" : proporcion <= 0.4m ? "media" : "baja";
            if (Resultado.Estado == "Experimental" && nivel == "alta") nivel = "media";
            return nivel;
        }
    }

    /// <summary>De dónde sale el número, explicado para alguien que no maneja sistemas.</summary>
    public string ExplicacionSencilla
    {
        get
        {
            if (Resultado.Demanda is not decimal demanda)
                return "Para calcular, el sistema necesita ver las ventas de este producto durante varias semanas. "
                    + "Mientras más ventas registres, mejor será el cálculo.";
            var semanas = Math.Max(1, Resultado.Dias / 7);
            var texto = $"El sistema revisó las ventas de este producto de las últimas {semanas} semanas. "
                + "Se fijó en qué días se vende más y en qué días menos (por ejemplo, los fines de semana), "
                + $"y con eso calculó que la semana siguiente se podrían vender unas {Cantidad(demanda, estimada: true)}.";
            if (Resultado.ErrorModelo is decimal error)
                texto += $" Cuando se puso a prueba con semanas anteriores, se equivocó en promedio por unas {Cantidad(error, estimada: true)} por semana.";
            return texto;
        }
    }
    public string TextoConfianza => Confianza switch
    {
        "alta" => "Confianza alta: el cálculo suele acertar con este producto.",
        "media" => "Confianza media: úsalo como guía, puede variar un poco.",
        "baja" => "Confianza baja: las ventas de este producto cambian mucho de una semana a otra. Revísalo tú también.",
        _ => ""
    };
    public bool TieneConfianza => !string.IsNullOrEmpty(TextoConfianza);

    private string NivelAcierto(decimal demanda)
    {
        if (Resultado.VentaReal is not decimal real) return "Neutro";
        var estimado = Resultado.Unidad == "kg" ? demanda : decimal.Ceiling(demanda);
        var proporcion = Math.Abs(real - estimado) / Math.Max(real, 1);
        return proporcion <= 0.2m ? "Bajo" : proporcion <= 0.4m ? "Medio" : "Alto";
    }

    private string SinCalculo() => Resultado.Estado switch
    {
        "Confirmar historial" => "Marca arriba la casilla «Registré todas mis ventas» y pulsa Revisar otra vez.",
        "Datos insuficientes" when Resultado.Dias < GenerarPrediccionDemandaUseCase.MinimoDias =>
            $"Todavía hay pocas ventas guardadas ({Resultado.Dias} días). Hacen falta unas 3 semanas para calcularlo. Sigue registrando sus ventas.",
        "Datos insuficientes" => "Este producto se vende pocos días. Sigue registrando sus ventas y pronto se podrá calcular.",
        "Revisar datos" => "Hay ventas de este producto con cantidades extrañas. Pide que las revisen.",
        _ => "No se pudo calcular esta vez. Pulsa Revisar de nuevo."
    };

    private string Cantidad(decimal valor, bool estimada = false)
    {
        if (Resultado.Unidad == "kg") return $"{valor.ToString("0.##", Cultura)} kg";
        // Evitar presentar fracciones de botellas. Es solo redondeo visual, no una orden de compra.
        if (estimada) valor = decimal.Ceiling(valor);
        return $"{valor.ToString("0.###", Cultura)} {(valor == 1 ? "unidad" : "unid.")}";
    }
}
