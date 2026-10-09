using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Caja.Enums;

namespace BodegaLuchito.Application.Caja.UseCases;

public sealed class ResolverDescuadreCajaUseCase
{
    private readonly ICajaRepository _cajaRepository;
    private readonly ISesionUsuario _sesionUsuario;

    public ResolverDescuadreCajaUseCase(
        ICajaRepository cajaRepository,
        ISesionUsuario sesionUsuario)
    {
        _cajaRepository = cajaRepository;
        _sesionUsuario = sesionUsuario;
    }

    public async Task EjecutarAsync(
        int sesionCajaId,
        string? observacion,
        CancellationToken cancellationToken = default)
    {
        var usuarioActual = _sesionUsuario.UsuarioActual;

        if (usuarioActual is null)
        {
            throw new InvalidOperationException(
                "No existe un usuario autenticado.");
        }

        if (!usuarioActual.EsAdministradora)
        {
            throw new UnauthorizedAccessException(
                "Solo una administradora puede resolver descuadres de caja.");
        }

        if (sesionCajaId <= 0)
        {
            throw new ArgumentException(
                "La sesión de caja no es válida.");
        }

        var sesion =
            await _cajaRepository.ObtenerSesionPorIdAsync(
                sesionCajaId,
                cancellationToken);

        if (sesion is null)
        {
            throw new InvalidOperationException(
                "La sesión de caja no existe.");
        }

        if (sesion.Estado != EstadoSesionCaja.Cerrada)
        {
            throw new InvalidOperationException(
                "Solo se pueden resolver descuadres de sesiones cerradas.");
        }

        if (!sesion.DiferenciaEfectivo.HasValue ||
            sesion.DiferenciaEfectivo.Value == 0)
        {
            throw new InvalidOperationException(
                "La sesión seleccionada no presenta un descuadre de caja.");
        }

        if (sesion.DescuadreResuelto)
        {
            throw new InvalidOperationException(
                "Este descuadre ya fue marcado como resuelto.");
        }

        sesion.DescuadreResuelto = true;
        sesion.FechaResolucionDescuadre = DateTime.Now;
        sesion.UsuarioResolucionId = usuarioActual.IdUsuario;
        sesion.ObservacionResolucionDescuadre =
            string.IsNullOrWhiteSpace(observacion)
                ? null
                : observacion.Trim();

        await _cajaRepository.GuardarCambiosAsync(
            cancellationToken);
    }
}
