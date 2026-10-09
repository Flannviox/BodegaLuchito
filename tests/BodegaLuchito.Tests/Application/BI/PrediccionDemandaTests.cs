using BodegaLuchito.Application.BI;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Infrastructure.BI;

namespace BodegaLuchito.Tests.Application.BI;

public sealed class PrediccionDemandaTests
{
    private static SesionUsuario Administradora()
    {
        var sesion = new SesionUsuario();
        sesion.IniciarSesion(new UsuarioSesion { IdUsuario = 1, Rol = RolUsuario.Administradora });
        return sesion;
    }

    private static HistorialDemanda Historial(int dias = 60) => new(1, "Papa", "kg", 20,
        DateTime.Today.AddDays(-dias), Enumerable.Range(0, dias)
            .Select(i => new VentaDiaria(DateTime.Today.AddDays(-dias + i), i + 0.125m)).ToArray());

    [Fact]
    public async Task Demostracion_NoConsultaBaseReal_YEjecutaSsa()
    {
        var repo = new Repositorio { Fallar = true };
        var caso = new GenerarPrediccionDemandaUseCase(repo, new ModeloDemandaSsa(), Administradora());
        var informe = await caso.EjecutarAsync(true, DateTime.Today, false);
        Assert.True(informe.Demostracion);
        Assert.Equal(4, informe.Productos.Count);
        Assert.Equal(3, informe.Productos.Count(p => p.Demanda.HasValue));
        Assert.All(informe.Productos.Where(p => p.Demanda.HasValue), p =>
        {
            Assert.Equal(15, p.Dias);
            Assert.Equal("Experimental", p.Estado);
            Assert.Equal(1, p.PeriodosEvaluados);
            Assert.True(p.Demanda >= 0);
            Assert.NotNull(p.ErrorModelo);
            Assert.NotNull(p.ErrorPromedio);
        });
        Assert.Equal("Datos insuficientes", informe.Productos.Single(p => p.ProductoId == 4).Estado);
    }

    [Theory]
    [InlineData(15, 1)]
    [InlineData(18, 2)]
    [InlineData(21, 3)]
    [InlineData(24, 4)]
    [InlineData(60, 4)]
    public async Task Piloto_ReservaFuturo_YNuncaEntrenaConMenosDeDoceDias(int dias, int bloques)
    {
        var datos = Historial(dias);
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(datos), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.Equal(bloques, fila.PeriodosEvaluados);
        Assert.Equal(dias < 60 ? "Experimental" : "Estimación referencial", fila.Estado);
        Assert.Equal(bloques + 1, modelo.Entrenamientos.Count);
        for (var i = 0; i < bloques; i++)
        {
            var corte = dias - bloques * 3 + i * 3;
            Assert.True(corte >= 12);
            Assert.Equal(datos.Ventas.Take(corte).Select(v => v.Cantidad), modelo.Entrenamientos[i]);
        }
        var esperado = Enumerable.Range(0, bloques).Average(i => Math.Abs(
            datos.Ventas.Skip(dias - bloques * 3 + i * 3).Take(3).Sum(v => v.Cantidad) - 0.75m));
        Assert.Equal(esperado, fila.ErrorModelo);
        Assert.Equal(datos.Ventas.Select(v => v.Cantidad), modelo.Entrenamientos.Last());
    }

    [Theory]
    [InlineData(15)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(60)]
    public async Task SsaReal_EntrenaYPronosticaEnLimitesDeConfiguracion(int dias)
    {
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial(dias)),
            new ModeloDemandaSsa(), Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.NotNull(fila.Demanda);
        Assert.NotNull(fila.ErrorModelo);
        Assert.True(fila.Demanda >= 0);
        Assert.Equal(dias < 60 ? "Experimental" : "Estimación referencial", fila.Estado);
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    public async Task QuinceDiasEsElLimiteDelPiloto(int dias, bool predice)
    {
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial(dias)), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.Equal(predice, fila.Demanda.HasValue);
        if (!predice) Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task VentasRecientesSinPasadoSuficiente_NoGeneranPiloto()
    {
        var datos = Historial(15);
        datos = datos with { Ventas = datos.Ventas.TakeLast(7).ToArray() };
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(datos), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.Equal("Datos insuficientes", fila.Estado);
        Assert.Null(fila.Demanda);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task PilotoTambienExigeConfirmarHistorial()
    {
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial(15)), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), false)).Productos);
        Assert.Equal("Confirmar historial", fila.Estado);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task HistorialCorto_NoInvocaModelo_NiInventaRiesgo()
    {
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial(5)), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.Null(fila.Demanda);
        Assert.Equal("—", fila.Riesgo);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task HistorialSinConfirmar_NoInterpretaAusenciasComoCeros()
    {
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial()), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), false)).Productos);
        Assert.Equal("Confirmar historial", fila.Estado);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task EvaluacionTemporal_NoFiltraFuturo_YComparaTotalesDeTresDias()
    {
        var modelo = new ModeloEspia();
        var datos = Historial();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(datos), modelo, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.Equal(new[] {48, 51, 54, 57, 60}, modelo.Entrenamientos.Select(x => x.Length));
        for (var i = 0; i < 4; i++)
            Assert.Equal(datos.Ventas.Take(48 + i * 3).Select(x => x.Cantidad), modelo.Entrenamientos[i]);
        Assert.Equal(0.75m, fila.Demanda);
        var error = Enumerable.Range(0, 4).Average(i =>
            Math.Abs(datos.Ventas.Skip(48 + i * 3).Take(3).Sum(x => x.Cantidad) - 0.75m));
        Assert.Equal(error, fila.ErrorModelo);
    }

    [Fact]
    public async Task CompletaCerosSoloEnPeriodoConfirmado_YExcluyeHoy()
    {
        var datos = Historial();
        datos = datos with { Ventas = datos.Ventas.Where((_, i) => i != 5)
            .Append(new VentaDiaria(DateTime.Today, 9999)).ToArray() };
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(datos), modelo, Administradora());
        await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true);
        Assert.Equal(0, modelo.Entrenamientos.Last()[5]);
        Assert.DoesNotContain(9999m, modelo.Entrenamientos.Last());
    }

    [Fact]
    public async Task ModeloInvalido_NoPublicaCifras()
    {
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial()),
            new ModeloEspia { Salida = [-1, 0, 0] }, Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, DateTime.Today.AddDays(-90), true)).Productos);
        Assert.Null(fila.Demanda);
        Assert.Equal("No se pudo estimar", fila.Estado);
    }

    [Theory]
    [InlineData(0, 0, "Alto")]
    [InlineData(8, 11, "Alto")]
    [InlineData(11, 11, "Alto")]
    [InlineData(20, 14, "Medio")]
    [InlineData(35, 7, "Bajo")]
    public void RiesgoUsaUmbralesExplicitos(int stock, int demanda, string esperado) =>
        Assert.Equal(esperado, GenerarPrediccionDemandaUseCase.ClasificarRiesgo(stock, demanda));

    [Fact]
    public async Task RechazaAccesoSinAdministradora()
    {
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(), new ModeloEspia(), new SesionUsuario());
        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(true, DateTime.Today, true));
    }

    [Fact]
    public async Task PermiteCancelarSinMostrarErrorDeModelo()
    {
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(), new ModeloEspia(), Administradora());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caso.EjecutarAsync(true, DateTime.Today, true, cancelacion.Token));
    }

    private sealed class Repositorio(params HistorialDemanda[] datos) : IHistorialDemandaRepository
    {
        public bool Fallar { get; init; }
        public Task<IReadOnlyList<HistorialDemanda>> ConsultarAsync(DateTime desde, DateTime hastaExclusiva,
            CancellationToken cancellationToken = default) => Fallar ? throw new InvalidOperationException("No consultar demo en DB")
                : Task.FromResult<IReadOnlyList<HistorialDemanda>>(datos);
    }

    private sealed class ModeloEspia : IModeloDemanda
    {
        public List<decimal[]> Entrenamientos { get; } = [];
        public decimal[] Salida { get; init; } = [0.25m, 0.25m, 0.25m];
        public decimal[] Predecir(IReadOnlyList<decimal> cantidades, int horizonte)
        {
            Assert.Equal(3, horizonte);
            Entrenamientos.Add(cantidades.ToArray());
            return Salida;
        }
    }
}
