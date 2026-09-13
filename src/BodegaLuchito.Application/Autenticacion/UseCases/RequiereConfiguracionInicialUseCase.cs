using BodegaLuchito.Application.Autenticacion.Interfaces;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class RequiereConfiguracionInicialUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public RequiereConfiguracionInicialUseCase(
        IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<bool> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var existeUsuario =
            await _usuarioRepository.ExisteAlgunUsuarioAsync(
                cancellationToken);

        return !existeUsuario;
    }
}
