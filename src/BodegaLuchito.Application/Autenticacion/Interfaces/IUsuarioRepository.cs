using BodegaLuchito.Domain.Autenticacion.Entities;

namespace BodegaLuchito.Application.Autenticacion.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreUsuarioAsync(
        string nombreUsuario,
        int? excluirUsuarioId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAlgunUsuarioAsync(
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(
        CancellationToken cancellationToken = default);
}
