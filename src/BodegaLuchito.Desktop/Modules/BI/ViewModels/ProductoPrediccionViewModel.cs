using System.Globalization;
using BodegaLuchito.Application.BI;

namespace BodegaLuchito.Desktop.Modules.BI.ViewModels;

// Presentación para la dueña. Conserva los valores originales para la evaluación y el riesgo.
public sealed class ProductoPrediccionViewModel(ResultadoPrediccion resultado)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");
    public ResultadoPrediccion Resultado { get; } = resultado;
    public string Producto => Resultado.Producto;
    public string Riesgo => Resultado.Riesgo;
    public string Existencias => Cantidad(Resultado.Stock);
    public string VentaPosible => Resultado.Demanda is decimal demanda
        ? $"Aprox. {Cantidad(demanda, estimada: true)}" : "Aún no se puede calcular";
    public string Situacion => Resultado.Demanda is null ? "Aún sin cálculo"
        : Resultado.Stock <= 0 ? "Sin existencias" : Riesgo switch
        {
            "Alto" => "Podría faltar",
            "Medio" => "Quedaría poco",
            _ => "Alcanzaría"
        };
    public string Orientacion => Resultado.Demanda is null ? SinCalculo()
        : Resultado.Stock <= 0 ? "No quedan existencias. Revisa si necesitas reponer este producto."
        : Riesgo switch
        {
            "Alto" => "Lo que tienes podría no alcanzar. Revisa este producto antes de tu próxima compra.",
            "Medio" => "Podría alcanzar, pero quedaría poca reserva. Conviene estar pendiente de sus ventas.",
            _ => "Lo que tienes alcanzaría según este cálculo. Sigue revisando las ventas."
        };
    public string Explicacion => Resultado.Demanda is null ? SinCalculo()
        : $"Tienes {Existencias} y podrías vender aproximadamente {Cantidad(Resultado.Demanda.Value, estimada: true)} en los próximos tres días. {Orientacion}";
    public string Advertencia
    {
        get
        {
            if (Resultado.Demanda is null) return "";
            var aviso = Resultado.Estado == "Experimental"
                ? "Todavía hay pocos días registrados. Toma esta cifra solo como una orientación."
                : "Es una aproximación: las ventas reales pueden ser diferentes.";
            if (Resultado.ErrorModelo >= Resultado.ErrorPromedio)
                aviso += " En la comprobación reciente, el cálculo no acertó más que un promedio sencillo.";
            return aviso;
        }
    }

    private string SinCalculo() => Resultado.Estado switch
    {
        "Confirmar historial" => "Confirma arriba que registraste todas las ventas desde la fecha elegida y vuelve a revisar.",
        "Datos insuficientes" when Resultado.Dias < GenerarPrediccionDemandaUseCase.MinimoDias =>
            $"Hay {Resultado.Dias} días completos registrados; se necesitan al menos 15 para intentarlo. Sigue registrando las ventas de este producto.",
        "Datos insuficientes" => "Aún faltan ventas suficientes o variadas para calcularlo. Sigue registrando las ventas de este producto.",
        "Revisar datos" => "Hay cantidades que necesitan revisión en las ventas registradas de este producto.",
        _ => "No se pudo calcular. Vuelve a revisar los productos; si continúa, solicita ayuda."
    };

    private string Cantidad(decimal valor, bool estimada = false)
    {
        if (Resultado.Unidad == "kg") return $"{valor.ToString("0.###", Cultura)} kg";
        // Evitar presentar fracciones de botellas. Es solo redondeo visual, no una orden de compra.
        if (estimada) valor = decimal.Ceiling(valor);
        return $"{valor.ToString("0.###", Cultura)} {(valor == 1 ? "unidad" : "unidades")}";
    }
}
