using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Ventas.Interfaces;

namespace BodegaLuchito.Application.Ventas.UseCases;

public class AnularVentaUseCase
{
    private readonly IVentaRepository _ventaRepository;
    private readonly ICajaRepository _cajaRepository;

    public AnularVentaUseCase(IVentaRepository ventaRepository, ICajaRepository cajaRepository)
    {
        _ventaRepository = ventaRepository;
        _cajaRepository = cajaRepository;
    }

    public async Task ExecuteAsync(AnularVentaRequest request, CancellationToken cancellationToken = default)
    {
        if (request.VentaId <= 0)
            throw new ArgumentException("El ID de la venta es inválido.");

        if (string.IsNullOrWhiteSpace(request.Motivo) || request.Motivo.Length > 250)
            throw new ArgumentException("El motivo es obligatorio y no debe exceder los 250 caracteres.");

        if (request.UsuarioAnulacionId <= 0)
            throw new ArgumentException("Se requiere un usuario válido para la anulación.");

        var sesionAbierta = await _cajaRepository.ObtenerSesionAbiertaAsync(cancellationToken);
        if (sesionAbierta == null)
            throw new InvalidOperationException("No se puede anular la venta porque no hay una sesión de caja abierta.");
        var venta = await _ventaRepository.ObtenerPorIdAsync(request.VentaId, cancellationToken);
        if (venta == null)
            throw new InvalidOperationException("La venta no existe.");

        if (venta.Anulado)
            throw new InvalidOperationException("La venta ya se encuentra anulada.");

        venta.Anulado = true;
        venta.MotivoAnulacion = request.Motivo.Trim();
        venta.FechaAnulacion = DateTime.Now;
        venta.UsuarioAnulacionId = request.UsuarioAnulacionId;

        //  Enviar a infraestructura
        await _ventaRepository.AnularVentaAsync(venta, sesionAbierta.Id, cancellationToken);
    }
}
