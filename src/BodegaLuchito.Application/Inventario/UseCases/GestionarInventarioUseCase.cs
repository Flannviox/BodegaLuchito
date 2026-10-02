using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Application.Inventario.Interfaces;
namespace BodegaLuchito.Application.Inventario.UseCases;

public sealed class GestionarInventarioUseCase(IInventarioRepository repositorio, ISesionUsuario sesion)
{
    private int ValidarAcceso()
    {
        var usuario = sesion.UsuarioActual;
        if (usuario is null || !usuario.EsAdministradora)
            throw new UnauthorizedAccessException("Solo la administradora puede gestionar el inventario.");
        return usuario.IdUsuario;
    }

    public Task<IReadOnlyList<ExistenciaInventario>> ConsultarExistenciasAsync(CancellationToken ct = default)
    {
        ValidarAcceso();
        return repositorio.ObtenerExistenciasAsync(ct);
    }

    public Task<IReadOnlyList<MovimientoInventarioDetalle>> ConsultarHistorialAsync(CancellationToken ct = default)
    {
        ValidarAcceso();
        return repositorio.ObtenerHistorialCompletoAsync(ct);
    }

    public Task RegistrarAsync(RegistrarMovimientoManualRequest solicitud, CancellationToken ct = default)
        => repositorio.RegistrarMovimientoManualAsync(solicitud, ValidarAcceso(), ct);
}
