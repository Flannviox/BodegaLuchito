using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Inventario.Interfaces;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Domain.Inventario;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Infrastructure.Productos.Repositories;

namespace BodegaLuchito.Infrastructure.Inventario.Repositories;

public sealed class InventarioRepository : IInventarioRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public InventarioRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ExistenciaInventario>> ObtenerExistenciasAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Productos.AsNoTracking().OrderBy(x => x.Nombre)
            .Select(x => new ExistenciaInventario(x.Id, x.Nombre, x.CodigoBarras, x.Categoria.Nombre,
                x.UnidadVenta, x.StockActual, x.StockMinimo, x.Activo,
                x.Activo && x.StockActual == 0 &&
                !context.MovimientosInventario.Any(m => m.ProductoId == x.Id) &&
                !context.DetallesAbastecimiento.Any(d => d.ProductoId == x.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoInventarioDetalle>> ObtenerHistorialCompletoAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.MovimientosInventario.AsNoTracking()
            .OrderByDescending(x => x.FechaHora).ThenByDescending(x => x.Id)
            .Select(x => new MovimientoInventarioDetalle(x.Id, x.ProductoId, x.Producto!.Nombre,
                x.FechaHora, x.Tipo, x.Cantidad, x.StockAnterior, x.StockPosterior,
                x.Producto.UnidadVenta == UnidadVenta.Peso ? "kg" : "unid",
                context.Set<Usuario>().Where(u => u.Id == x.UsuarioId).Select(u => u.NombreCompleto).FirstOrDefault() ?? "Usuario no disponible",
                x.Descripcion ?? "", x.AbastecimientoId))
            .ToListAsync(cancellationToken);
    }

    public async Task RegistrarMovimientoManualAsync(RegistrarMovimientoManualRequest solicitud,
        int usuarioId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Stock y movimiento se validan y guardan en la misma transacción.
        await using var transaccion = await context.Database.BeginTransactionAsync(cancellationToken);
        if (!await context.Set<Usuario>().AnyAsync(u => u.Id == usuarioId && u.Activo &&
            u.Rol == RolUsuario.Administradora, cancellationToken))
            throw new UnauthorizedAccessException("La administradora debe tener una sesión activa.");
        Producto producto;
        if (solicitud.ProductoNuevo is not null)
        {
            if (solicitud.Tipo != TipoMovimientoInventario.SaldoInicial || solicitud.ProductoId != 0 || solicitud.StockEsperado != 0)
                throw new ArgumentException("Un producto nuevo debe ingresar mediante su saldo inicial.");
            producto = PrepararProductoNuevo.Crear(solicitud.ProductoNuevo);
            await AltaProductoEnOperacion.AgregarAsync(context, producto, cancellationToken);
        }
        else
            producto = await context.Productos.SingleOrDefaultAsync(x => x.Id == solicitud.ProductoId, cancellationToken)
                ?? throw new InvalidOperationException("El producto ya no está disponible.");
        var tieneOperaciones = await context.MovimientosInventario.AnyAsync(x => x.ProductoId == producto.Id, cancellationToken)
            || await context.DetallesAbastecimiento.AnyAsync(x => x.ProductoId == producto.Id, cancellationToken);
        var posterior = ReglasMovimientoManual.CalcularStockPosterior(producto, solicitud.Tipo,
            solicitud.Cantidad, solicitud.StockEsperado, tieneOperaciones, solicitud.Motivo);
        context.MovimientosInventario.Add(new MovimientoInventario
        {
            Producto = producto, UsuarioId = usuarioId, Tipo = solicitud.Tipo,
            Cantidad = Math.Abs(posterior - producto.StockActual),
            StockAnterior = producto.StockActual, StockPosterior = posterior,
            FechaHora = DateTime.Now, Descripcion = solicitud.Motivo.Trim()
        });
        producto.StockActual = posterior;
        await context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoInventario>> ObtenerMovimientosAsync(
        CancellationToken cancellationToken = default, TipoMovimientoInventario? tipo = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<MovimientoInventario>()
            .AsNoTracking()
            .Include(x => x.Producto)
            .Where(x => !tipo.HasValue || x.Tipo == tipo.Value)
            .OrderByDescending(x => x.FechaHora)
            .ThenByDescending(x => x.Id)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

}
