namespace BodegaLuchito.Application.BI;

/// <summary>Datos sintéticos deterministas en memoria. Nunca se insertan en ventas o inventario.</summary>
public static class DatosDemostracionDemanda
{
    public const int Dias = 15;

    public static IReadOnlyList<HistorialDemanda> Crear(DateTime hoy)
    {
        var desde = hoy.Date.AddDays(-Dias);
        return [
            CrearProducto(1, "Leche · ejemplo", "unid", 8, 4, desde, Dias),
            CrearProducto(2, "Agua · ejemplo", "unid", 25, 5, desde, Dias),
            CrearProducto(3, "Papa · ejemplo", "kg", 80, 3, desde, Dias),
            CrearProducto(4, "Producto nuevo · ejemplo", "unid", 10, 2, hoy.Date.AddDays(-8), 8)
        ];
    }

    private static HistorialDemanda CrearProducto(int id, string nombre, string unidad,
        decimal stock, decimal baseDiaria, DateTime desde, int dias)
    {
        var aleatorio = new Random(1700 + id);
        var ventas = Enumerable.Range(0, dias).Select(i =>
        {
            var fecha = desde.AddDays(i);
            var finSemana = fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var cantidad = baseDiaria + (finSemana ? 3 : 0) + aleatorio.Next(0, 3);
            if (unidad == "kg") cantidad += aleatorio.Next(0, 4) * 0.25m;
            return new VentaDiaria(fecha, cantidad);
        }).ToArray();
        return new(id, nombre, unidad, stock, desde, ventas);
    }
}
