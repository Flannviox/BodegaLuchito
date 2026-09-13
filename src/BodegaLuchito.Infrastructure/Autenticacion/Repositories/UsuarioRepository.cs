using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Infrastructure.Autenticacion.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public UsuarioRepository(
        IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<Usuario>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<Usuario>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NombreUsuario == nombreUsuario,
                cancellationToken);
    }

    public async Task<bool> ExisteNombreUsuarioAsync(
        string nombreUsuario,
        int? excluirUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<Usuario>()
            .AnyAsync(
                x => x.NombreUsuario == nombreUsuario
                     && (!excluirUsuarioId.HasValue
                         || x.Id != excluirUsuarioId.Value),
                cancellationToken);
    }

    public async Task<bool> ExisteAlgunUsuarioAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<Usuario>()
            .AnyAsync(cancellationToken);
    }

    public async Task AgregarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Set<Usuario>().Add(usuario);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Set<Usuario>().Update(usuario);

        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(
    CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.Set<Usuario>()
            .AsNoTracking()
            .OrderBy(x => x.NombreCompleto)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarAdministradorasActivasAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.Set<Usuario>()
            .CountAsync(
                x => x.Rol == RolUsuario.Administradora
                     && x.Activo,
                cancellationToken);
    }
}
