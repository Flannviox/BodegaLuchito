using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;

namespace BodegaLuchito.Application.Caja.UseCases;

public class ConsultarHistorialCierresUseCase(ICajaRepository cajaRepository, IUsuarioRepository usuarioRepository)
{
    public async Task<IReadOnlyList<HistorialCierreResult>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var cierres = await cajaRepository.ObtenerHistorialCierresAsync(cancellationToken);
        var usuarios = await usuarioRepository.ObtenerTodosAsync(cancellationToken);
        var nombres = usuarios.ToDictionary(x => x.Id, x => string.IsNullOrWhiteSpace(x.NombreCompleto)
            ? x.NombreUsuario : x.NombreCompleto);
        string Nombre(int? id) => id.HasValue
            ? nombres.GetValueOrDefault(id.Value, "Usuario no disponible") : "Sin registro";
        return cierres.Select(x => new HistorialCierreResult(x, Nombre(x.UsuarioAperturaId), Nombre(x.UsuarioCierreId))).ToList();
    }
}
