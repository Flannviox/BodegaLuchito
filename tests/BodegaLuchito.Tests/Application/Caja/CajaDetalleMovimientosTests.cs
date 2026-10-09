using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Abastecimiento.Entities;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EntidadAbastecimiento =
    BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Tests.Application.Caja
{
    public sealed class CajaDetalleMovimientosTests
    {
        private static async Task<(
            SqliteConnection connection,
            DbContextOptions<BodegaLuchitoDbContext> options)>
            CrearBaseEnMemoriaAsync()
        {
            var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                    .UseSqlite(connection)
                    .Options;

            await using var context =
                new BodegaLuchitoDbContext(options);

            await context.Database.EnsureCreatedAsync();

            return (connection, options);
        }

        [Fact]
        public async Task ObtenerDetalleMovimientos_DebeRecuperarVentaYAbastecimientoConProductos()
        {
            var (connection, options) =
                await CrearBaseEnMemoriaAsync();

            await using var connectionDispose = connection;

            await using var context =
                new BodegaLuchitoDbContext(options);

            var usuario = new Usuario
            {
                NombreCompleto = "Usuario Prueba",
                NombreUsuario = "usuario-caja",
                PasswordHash = "hash-prueba",
                Rol = RolUsuario.Administradora,
                Activo = true
            };

            var categoria = new Categoria
            {
                Nombre = "Abarrotes"
            };

            var producto = new Producto
            {
                Nombre = "Arroz",
                Categoria = categoria,
                PrecioVenta = 5m,
                UnidadVenta = UnidadVenta.Unidad,
                ControlaInventario = true,
                StockActual = 20m,
                StockMinimo = 2m,
                Activo = true
            };

            var proveedor = new Proveedor
            {
                Nombre = "Proveedor Prueba",
                Ruc = "20123456789",
                Activo = true
            };

            context.Set<Usuario>().Add(usuario);
            context.Set<Producto>().Add(producto);
            context.Set<Proveedor>().Add(proveedor);

            await context.SaveChangesAsync();

            var sesion = new SesionCaja
            {
                UsuarioAperturaId = usuario.Id,
                FechaApertura = DateTime.Now,
                FondoInicial = 100m,
                Estado = EstadoSesionCaja.Abierta
            };

            context.Set<SesionCaja>().Add(sesion);

            var venta = new Venta
            {
                UsuarioId = usuario.Id,
                FechaHora = DateTime.Now,
                Subtotal = 10m,
                IGV = 0m,
                Total = 10m,
                MetodoPago = MetodoPago.Yape,
                Detalles =
                [
                    new DetalleVenta
                {
                    ProductoId = producto.Id,
                    Cantidad = 2m,
                    PrecioUnitario = 5m,
                    Subtotal = 10m
                }
                ]
            };

            var abastecimiento = new EntidadAbastecimiento
            {
                ProveedorId = proveedor.Id,
                FechaHora = DateTime.Now,
                Total = 12m,
                MetodoPago = MetodoPago.Plin,
                Detalles =
                [
                    new DetalleAbastecimiento
                {
                    ProductoId = producto.Id,
                    Cantidad = 3m,
                    CostoUnitario = 4m,
                    TotalLinea = 12m
                }
                ]
            };

            context.Set<Venta>().Add(venta);
            context.Set<EntidadAbastecimiento>().Add(abastecimiento);

            await context.SaveChangesAsync();

            context.Set<MovimientoCaja>().AddRange(
                new MovimientoCaja
                {
                    SesionCajaId = sesion.Id,
                    UsuarioId = usuario.Id,
                    Tipo = TipoMovimientoCaja.IngresoVenta,
                    MetodoPago = MetodoPago.Yape,
                    Monto = venta.Total,
                    FechaHora = venta.FechaHora,
                    VentaId = venta.Id,
                    Descripcion = $"Ingreso por venta #{venta.Id}"
                },
                new MovimientoCaja
                {
                    SesionCajaId = sesion.Id,
                    UsuarioId = usuario.Id,
                    Tipo = TipoMovimientoCaja.EgresoAbastecimiento,
                    MetodoPago = MetodoPago.Plin,
                    Monto = abastecimiento.Total,
                    FechaHora = abastecimiento.FechaHora,
                    AbastecimientoId = abastecimiento.Id,
                    Descripcion =
                        $"Pago por abastecimiento #{abastecimiento.Id}"
                });

            await context.SaveChangesAsync();

            var repository =
                new CajaRepository(context);

            var resultado =
                await repository.ObtenerDetalleMovimientosPorSesionAsync(
                    sesion.Id);

            Assert.Equal(2, resultado.Count);

            var movimientoVenta =
            Assert.Single(resultado, x => x.VentaId == venta.Id);           

            Assert.Equal(
                TipoMovimientoCaja.IngresoVenta,
                movimientoVenta.Tipo);

            Assert.Equal(
                MetodoPago.Yape,
                movimientoVenta.MetodoPago);

            Assert.Equal(10m, movimientoVenta.Monto);

            var detalleVenta =
                Assert.Single(movimientoVenta.Detalles);

            Assert.Equal("Arroz", detalleVenta.ProductoNombre);
            Assert.Equal(2m, detalleVenta.Cantidad);
            Assert.Equal(5m, detalleVenta.PrecioUnitario);
            Assert.Equal(10m, detalleVenta.Subtotal);

            var movimientoAbastecimiento =
                Assert.Single(
                    resultado,
                    x => x.AbastecimientoId == abastecimiento.Id);

            Assert.Equal(
                TipoMovimientoCaja.EgresoAbastecimiento,
                movimientoAbastecimiento.Tipo);

            Assert.Equal(
                MetodoPago.Plin,
                movimientoAbastecimiento.MetodoPago);

            Assert.Equal(12m, movimientoAbastecimiento.Monto);

            var detalleAbastecimiento =
                Assert.Single(
                    movimientoAbastecimiento.Detalles);

            Assert.Equal(
                "Arroz",
                detalleAbastecimiento.ProductoNombre);

            Assert.Equal(3m, detalleAbastecimiento.Cantidad);
            Assert.Equal(4m, detalleAbastecimiento.PrecioUnitario);
            Assert.Equal(12m, detalleAbastecimiento.Subtotal);
        }
        [Fact]
        public async Task ObtenerDetalleMovimientos_NoDebeMezclarSesionesDeCaja()
        {
            var (connection, options) =
                await CrearBaseEnMemoriaAsync();

            await using var connectionDispose = connection;

            await using var context =
                new BodegaLuchitoDbContext(options);

            var sesionUno = new SesionCaja
            {
                UsuarioAperturaId = 1,
                FechaApertura = DateTime.Now.AddHours(-2),
                FondoInicial = 100m,
                Estado = EstadoSesionCaja.Cerrada
            };

            var sesionDos = new SesionCaja
            {
                UsuarioAperturaId = 2,
                FechaApertura = DateTime.Now,
                FondoInicial = 200m,
                Estado = EstadoSesionCaja.Abierta
            };

            context.Set<SesionCaja>().AddRange(
                sesionUno,
                sesionDos);

            await context.SaveChangesAsync();

            var movimientoSesionUno = new MovimientoCaja
            {
                SesionCajaId = sesionUno.Id,
                UsuarioId = 1,
                Tipo = TipoMovimientoCaja.IngresoVenta,
                MetodoPago = MetodoPago.Efectivo,
                Monto = 10m,
                FechaHora = DateTime.Now.AddHours(-1),
                Descripcion = "Movimiento sesión uno"
            };

            var movimientoSesionDos = new MovimientoCaja
            {
                SesionCajaId = sesionDos.Id,
                UsuarioId = 2,
                Tipo = TipoMovimientoCaja.IngresoVenta,
                MetodoPago = MetodoPago.Yape,
                Monto = 25m,
                FechaHora = DateTime.Now,
                Descripcion = "Movimiento sesión dos"
            };

            context.Set<MovimientoCaja>().AddRange(
                movimientoSesionUno,
                movimientoSesionDos);

            await context.SaveChangesAsync();

            var repository =
                new CajaRepository(context);

            var resultado =
                await repository.ObtenerDetalleMovimientosPorSesionAsync(
                    sesionUno.Id);

            var movimiento =
                Assert.Single(resultado);

            Assert.Equal(
                movimientoSesionUno.Id,
                movimiento.MovimientoCajaId);

            Assert.Equal(10m, movimiento.Monto);

            Assert.Equal(
                MetodoPago.Efectivo,
                movimiento.MetodoPago);

            Assert.DoesNotContain(
                resultado,
                x => x.MovimientoCajaId == movimientoSesionDos.Id);
        }
    }
}
