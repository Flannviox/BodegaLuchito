using System.Globalization;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Ventas.UseCases;

public sealed record ResultadoCobro(bool Valido, decimal Recibido, decimal Vuelto, string Mensaje);

public static class EvaluarCobroUseCase
{
    public static ResultadoCobro Ejecutar(decimal total, MetodoPago medio, string? texto)
    {
        if (total <= 0 || decimal.Round(total, 2) != total)
            return new(false, 0, 0, "Revisa el total de la venta.");
        if (!Enum.IsDefined(medio)) return new(false, 0, 0, "Selecciona un medio de pago válido.");
        if (medio != MetodoPago.Efectivo)
            return new(true, total, 0, "Verifica que el pago recibido coincida con el total de la venta.");
        var valor = (texto ?? "").Trim().Replace(',', '.');
        var partes = valor.Split('.');
        if (valor.Length == 0 || partes.Length > 2 || partes[0].Length == 0 ||
            partes.Any(p => p.Any(c => c < '0' || c > '9')) ||
            (partes.Length == 2 && (partes[1].Length == 0 || partes[1].Length > 2)) ||
            !decimal.TryParse(valor, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var recibido))
            return new(false, 0, 0, "Ingresa un monto válido con hasta dos decimales, sin separadores de miles.");
        if (recibido < total) return new(false, recibido, 0, $"Faltan S/ {total - recibido:N2} para completar el pago.");
        return new(true, recibido, recibido - total, "El efectivo cubre el total de la venta.");
    }
}
