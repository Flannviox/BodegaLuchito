using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Autenticacion.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly BodegaLuchitoDbContext _context;

    public UsuarioRepository(
        BodegaLuchitoDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Usuario>()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Usuario>()
            .SingleOrDefaultAsync(
                x => x.NombreUsuario == nombreUsuario,
                cancellationToken);
    }

    public async Task<bool> ExisteNombreUsuarioAsync(
        string nombreUsuario,
        int? excluirUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Usuario>()
            .AnyAsync(
                x => x.NombreUsuario == nombreUsuario
                     && (!excluirUsuarioId.HasValue
                         || x.Id != excluirUsuarioId.Value),
                cancellationToken);
    }

    public async Task<bool> ExisteAlgunUsuarioAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Usuario>()
            .AnyAsync(cancellationToken);
    }

    public async Task AgregarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<Usuario>()
            .AddAsync(usuario, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task GuardarCambiosAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
