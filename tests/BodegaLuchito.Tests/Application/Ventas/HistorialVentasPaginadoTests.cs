using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Ventas.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.Ventas;

public sealed class HistorialVentasPaginadoTests
{
    // 25 ventas: una por día, del 01/10/2026 al 25/10/2026, a las 10:00.
    private static readonly DateTime Inicio = new(2026, 10, 1);

    private static async Task<(VentaRepository repo, string ruta)> CrearAsync()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"historial-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite($"Data Source={ruta};Pooling=False").Options;
        await using (var db = new BodegaLuchitoDbContext(opciones))
        {
            await db.Database.MigrateAsync();
            db.Add(new Usuario { Id = 1, NombreUsuario = "prueba", NombreCompleto = "Prueba", PasswordHash = "prueba" });
            db.Ventas.AddRange(Enumerable.Range(0, 25).Select(i => new Venta
            {
                UsuarioId = 1, FechaHora = Inicio.AddDays(i).AddHours(10), Subtotal = 10, Total = 10
            }));
            await db.SaveChangesAsync();
        }
        return (new VentaRepository(new TestDbContextFactory(opciones)), ruta);
    }

    [Fact]
    public async Task SinFiltro_PaginaDeMasRecienteAMasAntigua()
    {
        var (repo, ruta) = await CrearAsync();
        try
        {
            var primera = await repo.ObtenerHistorialPaginadoAsync(null, null, 1, 10);
            Assert.Equal(25, primera.TotalVentas);
            Assert.Equal(3, primera.TotalPaginas);
            Assert.Equal(10, primera.Ventas.Count);
            Assert.Equal(Inicio.AddDays(24).AddHours(10), primera.Ventas[0].FechaHora);
            Assert.NotNull(primera.Ventas[0].Usuario);

            var ultima = await repo.ObtenerHistorialPaginadoAsync(null, null, 3, 10);
            Assert.Equal(5, ultima.Ventas.Count);
            Assert.Equal(Inicio.AddHours(10), ultima.Ventas[^1].FechaHora);
        }
        finally { File.Delete(ruta); }
    }

    [Fact]
    public async Task FiltroDeFechas_IncluyeDiasCompletosEnAmbosExtremos()
    {
        var (repo, ruta) = await CrearAsync();
        try
        {
            var pagina = await repo.ObtenerHistorialPaginadoAsync(new DateTime(2026, 10, 5, 18, 0, 0),
                new DateTime(2026, 10, 7), 1, 10);
            Assert.Equal(3, pagina.TotalVentas);
            Assert.Equal(new[] { 7, 6, 5 }, pagina.Ventas.Select(v => v.FechaHora.Day));

            var soloDesde = await repo.ObtenerHistorialPaginadoAsync(new DateTime(2026, 10, 21), null, 1, 10);
            Assert.Equal(5, soloDesde.TotalVentas);
            var soloHasta = await repo.ObtenerHistorialPaginadoAsync(null, new DateTime(2026, 10, 2), 1, 10);
            Assert.Equal(2, soloHasta.TotalVentas);
        }
        finally { File.Delete(ruta); }
    }

    [Fact]
    public async Task PaginaFueraDeRango_DevuelveLaUltimaDisponible_YSinResultadosEsPaginaUno()
    {
        var (repo, ruta) = await CrearAsync();
        try
        {
            var pagina = await repo.ObtenerHistorialPaginadoAsync(null, null, 99, 10);
            Assert.Equal(3, pagina.Pagina);
            Assert.Equal(5, pagina.Ventas.Count);

            var vacia = await repo.ObtenerHistorialPaginadoAsync(new DateTime(2027, 1, 1), null, 4, 10);
            Assert.Equal(0, vacia.TotalVentas);
            Assert.Equal(1, vacia.Pagina);
            Assert.Equal(1, vacia.TotalPaginas);
            Assert.Empty(vacia.Ventas);
        }
        finally { File.Delete(ruta); }
    }
}
