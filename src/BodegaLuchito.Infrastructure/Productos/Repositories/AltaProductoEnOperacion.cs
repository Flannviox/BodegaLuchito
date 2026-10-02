using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Productos.Repositories;

// Usa el contexto de la operación: no guarda ni abre otra transacción.
internal static class AltaProductoEnOperacion
{
    internal static async Task AgregarAsync(BodegaLuchitoDbContext contexto, Producto producto, CancellationToken ct)
    {
        if (!await contexto.Categorias.AnyAsync(x => x.Id == producto.CategoriaId && x.Activo, ct))
            throw new InvalidOperationException("La categoría seleccionada ya no está activa.");
        if (producto.CodigoBarras is { } codigo &&
            (contexto.Productos.Local.Any(x => x.CodigoBarras == codigo) ||
             await contexto.Productos.AnyAsync(x => x.CodigoBarras == codigo, ct)))
            throw new InvalidOperationException($"El código '{codigo}' ya está registrado. Selecciona el producto existente.");
        contexto.Productos.Add(producto);
    }
}
