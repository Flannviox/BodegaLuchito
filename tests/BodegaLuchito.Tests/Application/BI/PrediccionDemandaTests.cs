using BodegaLuchito.Application.BI;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Infrastructure.BI;

namespace BodegaLuchito.Tests.Application.BI;

public sealed class PrediccionDemandaTests
{
    private static DateTime Lunes => GenerarPrediccionDemandaUseCase.InicioSemana(DateTime.Today);
    private static DateTime SemanaPasada => GenerarPrediccionDemandaUseCase.UltimaSemanaCerrada(DateTime.Today);
    private static DateTime DesdeAmplio => DateTime.Today.AddDays(-150);

    private static SesionUsuario Administradora()
    {
        var sesion = new SesionUsuario();
        sesion.IniciarSesion(new UsuarioSesion { IdUsuario = 1, Rol = RolUsuario.Administradora });
        return sesion;
    }

    /// <summary>Historial diario continuo que termina el domingo anterior a <paramref name="fin"/> (por defecto, el lunes actual).</summary>
    private static HistorialDemanda Historial(int dias = 70, DateTime? fin = null, bool controla = true)
    {
        var corte = fin ?? Lunes;
        return new(1, "Papa", "kg", 20, corte.AddDays(-dias), Enumerable.Range(0, dias)
            .Select(i => new VentaDiaria(corte.AddDays(-dias + i), i + 0.125m)).ToArray(), controla);
    }

    private static decimal Suma(HistorialDemanda datos, DateTime desde, DateTime hasta) =>
        datos.Ventas.Where(v => v.Fecha >= desde && v.Fecha < hasta).Sum(v => v.Cantidad);

    private static async Task<ResultadoPrediccion> UnicoAsync(HistorialDemanda datos, ModeloEspia modelo, bool completo = true) =>
        Assert.Single((await new GenerarPrediccionDemandaUseCase(new Repositorio(datos), modelo, Administradora())
            .EjecutarAsync(false, SemanaPasada, DesdeAmplio, completo)).Productos);

    [Fact]
    public void SemanasCerradas_SonDeLunesADomingo_YExcluyenLaSemanaEnCurso()
    {
        var semanas = GenerarPrediccionDemandaUseCase.SemanasCerradas(DateTime.Today);
        Assert.Equal(GenerarPrediccionDemandaUseCase.SemanasConsultables, semanas.Count);
        Assert.All(semanas, s => Assert.Equal(DayOfWeek.Monday, s.DayOfWeek));
        Assert.Equal(Lunes.AddDays(-7), semanas[0]);
        Assert.True(semanas[0].AddDays(7) <= DateTime.Today);
        Assert.Equal(new DateTime(2026, 10, 19), GenerarPrediccionDemandaUseCase.InicioSemana(new DateTime(2026, 10, 25)));
        Assert.Equal(new DateTime(2026, 10, 19), GenerarPrediccionDemandaUseCase.InicioSemana(new DateTime(2026, 10, 19, 18, 0, 0)));
    }

    [Fact]
    public async Task Demostracion_NoConsultaBaseReal_SoloIncluyeVendidos_YEjecutaSsa()
    {
        var repo = new Repositorio { Fallar = true };
        var caso = new GenerarPrediccionDemandaUseCase(repo, new ModeloDemandaSsa(), Administradora());
        var informe = await caso.EjecutarAsync(true, DateTime.Today, DateTime.Today, false);
        Assert.True(informe.Demostracion);
        Assert.Equal(SemanaPasada, informe.SemanaInicio);
        Assert.Equal(Lunes, informe.ProyeccionInicio);
        Assert.False(informe.ProyeccionTerminada);
        Assert.Equal(4, informe.Productos.Count);
        Assert.Equal(1, informe.ProductosSinVentas);
        Assert.DoesNotContain(informe.Productos, p => p.ProductoId == 5);
        Assert.Equal(3, informe.Productos.Count(p => p.Demanda.HasValue));
        Assert.All(informe.Productos, p => Assert.True(p.VendidoSemana > 0));
        Assert.All(informe.Productos.Where(p => p.Demanda.HasValue), p =>
        {
            Assert.Equal(56, p.Dias);
            Assert.Equal("Estimación referencial", p.Estado);
            Assert.Equal(4, p.SemanasEvaluadas);
            Assert.True(p.Demanda >= 0);
            Assert.NotNull(p.ErrorModelo);
            Assert.NotNull(p.ErrorSemanaAnterior);
            Assert.NotNull(p.VendidoSemanaAnterior);
            Assert.Null(p.VentaReal);
        });
        Assert.Equal("Datos insuficientes", informe.Productos.Single(p => p.ProductoId == 4).Estado);
    }

    [Theory]
    [InlineData(21, 1)]
    [InlineData(28, 2)]
    [InlineData(35, 3)]
    [InlineData(42, 4)]
    [InlineData(70, 4)]
    public async Task EvaluaSemanasAnteriores_SinFiltrarFuturo_YNuncaEntrenaConMenosDeCatorceDias(int dias, int bloques)
    {
        var datos = Historial(dias);
        var modelo = new ModeloEspia();
        var fila = await UnicoAsync(datos, modelo);
        Assert.Equal(bloques, fila.SemanasEvaluadas);
        Assert.Equal(dias < 56 ? "Experimental" : "Estimación referencial", fila.Estado);
        Assert.Equal(bloques + 1, modelo.Entrenamientos.Count);
        for (var i = 0; i < bloques; i++)
        {
            var corte = dias - bloques * 7 + i * 7;
            Assert.True(corte >= 14);
            Assert.Equal(datos.Ventas.Take(corte).Select(v => v.Cantidad), modelo.Entrenamientos[i]);
        }
        var esperado = Enumerable.Range(0, bloques).Average(i => Math.Abs(
            datos.Ventas.Skip(dias - bloques * 7 + i * 7).Take(7).Sum(v => v.Cantidad) - 1.75m));
        Assert.Equal(esperado, fila.ErrorModelo);
        Assert.Equal(datos.Ventas.Select(v => v.Cantidad), modelo.Entrenamientos.Last());
        Assert.Equal(1.75m, fila.Demanda);
    }

    [Fact]
    public async Task DeterminaLoVendidoEnLaSemanaCerrada_YEnLaSemanaPrevia()
    {
        var datos = Historial();
        var fila = await UnicoAsync(datos, new ModeloEspia());
        Assert.Equal(Suma(datos, SemanaPasada, Lunes), fila.VendidoSemana);
        Assert.Equal(Suma(datos, SemanaPasada.AddDays(-7), SemanaPasada), fila.VendidoSemanaAnterior);
        Assert.Null(fila.VentaReal);
        // Referencia simple: repetir la semana previa a cada semana evaluada.
        Assert.NotNull(fila.ErrorSemanaAnterior);
    }

    [Fact]
    public async Task ProductosSinVentasEnLaSemana_NoSeEstiman()
    {
        var vendido = Historial();
        var sinVentas = Historial() with
        {
            ProductoId = 2, Producto = "Sin ventas",
            Ventas = Historial().Ventas.Where(v => v.Fecha < SemanaPasada).ToArray()
        };
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(vendido, sinVentas), modelo, Administradora());
        var informe = await caso.EjecutarAsync(false, SemanaPasada, DesdeAmplio, true);
        Assert.Equal(1, Assert.Single(informe.Productos).ProductoId);
        Assert.Equal(1, informe.ProductosSinVentas);
        Assert.Equal(5, modelo.Entrenamientos.Count);
    }

    [Fact]
    public async Task SemanaAntigua_ComparaConLoVendidoRealmente_SinUsarloParaEntrenar()
    {
        var datos = Historial(77);
        var semana = SemanaPasada.AddDays(-7);
        var modelo = new ModeloEspia();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(datos), modelo, Administradora());
        var informe = await caso.EjecutarAsync(false, semana, DesdeAmplio, true);
        var fila = Assert.Single(informe.Productos);
        Assert.True(informe.ProyeccionTerminada);
        Assert.Equal(Suma(datos, semana, SemanaPasada), fila.VendidoSemana);
        Assert.Equal(Suma(datos, SemanaPasada, Lunes), fila.VentaReal);
        Assert.Equal(70, modelo.Entrenamientos.Last().Length);
        Assert.Equal("—", fila.Riesgo);
    }

    [Fact]
    public async Task ProductoSinControlDeInventario_SeEstimaSinRiesgo()
    {
        var fila = await UnicoAsync(Historial(controla: false), new ModeloEspia());
        Assert.NotNull(fila.Demanda);
        Assert.False(fila.ControlaInventario);
        Assert.Equal("—", fila.Riesgo);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(27)]
    [InlineData(28)]
    [InlineData(56)]
    [InlineData(70)]
    public async Task SsaReal_EntrenaYPronosticaUnaSemanaEnLimitesDeConfiguracion(int dias)
    {
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial(dias)),
            new ModeloDemandaSsa(), Administradora());
        var fila = Assert.Single((await caso.EjecutarAsync(false, SemanaPasada, DesdeAmplio, true)).Productos);
        Assert.NotNull(fila.Demanda);
        Assert.NotNull(fila.ErrorModelo);
        Assert.True(fila.Demanda >= 0);
        Assert.Equal(dias < 56 ? "Experimental" : "Estimación referencial", fila.Estado);
    }

    [Theory]
    [InlineData(20, false)]
    [InlineData(21, true)]
    public async Task TresSemanasEsElMinimo(int dias, bool predice)
    {
        var modelo = new ModeloEspia();
        var fila = await UnicoAsync(Historial(dias), modelo);
        Assert.Equal(predice, fila.Demanda.HasValue);
        if (!predice) Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task VentasRecientesSinPasadoSuficiente_NoGeneranEstimacion()
    {
        var datos = Historial(21);
        datos = datos with { Ventas = datos.Ventas.TakeLast(7).ToArray() };
        var modelo = new ModeloEspia();
        var fila = await UnicoAsync(datos, modelo);
        Assert.Equal("Datos insuficientes", fila.Estado);
        Assert.Null(fila.Demanda);
        Assert.True(fila.VendidoSemana > 0);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task HistorialSinConfirmar_NoInterpretaAusenciasComoCeros()
    {
        var modelo = new ModeloEspia();
        var fila = await UnicoAsync(Historial(), modelo, completo: false);
        Assert.Equal("Confirmar historial", fila.Estado);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task HistorialCorto_NoInvocaModelo_NiInventaRiesgo()
    {
        var modelo = new ModeloEspia();
        var fila = await UnicoAsync(Historial(10), modelo);
        Assert.Null(fila.Demanda);
        Assert.Equal("—", fila.Riesgo);
        Assert.Empty(modelo.Entrenamientos);
    }

    [Fact]
    public async Task EvaluacionTemporal_UsaBloquesDeSieteDias()
    {
        var modelo = new ModeloEspia();
        var datos = Historial();
        var fila = await UnicoAsync(datos, modelo);
        Assert.Equal(new[] { 42, 49, 56, 63, 70 }, modelo.Entrenamientos.Select(x => x.Length));
        for (var i = 0; i < 4; i++)
            Assert.Equal(datos.Ventas.Take(42 + i * 7).Select(x => x.Cantidad), modelo.Entrenamientos[i]);
        var error = Enumerable.Range(0, 4).Average(i =>
            Math.Abs(datos.Ventas.Skip(42 + i * 7).Take(7).Sum(x => x.Cantidad) - 1.75m));
        Assert.Equal(error, fila.ErrorModelo);
    }

    [Fact]
    public async Task CompletaCerosSoloEnPeriodoConfirmado_YExcluyeLaSemanaEnCurso()
    {
        var datos = Historial();
        datos = datos with { Ventas = datos.Ventas.Where((_, i) => i != 5)
            .Append(new VentaDiaria(Lunes, 9999)).ToArray() };
        var modelo = new ModeloEspia();
        await UnicoAsync(datos, modelo);
        Assert.Equal(0, modelo.Entrenamientos.Last()[5]);
        Assert.DoesNotContain(9999m, modelo.Entrenamientos.Last());
    }

    [Fact]
    public async Task ModeloInvalido_NoPublicaCifras()
    {
        var fila = await UnicoAsync(Historial(), new ModeloEspia { Salida = [-1, 0, 0, 0, 0, 0, 0] });
        Assert.Null(fila.Demanda);
        Assert.Equal("No se pudo estimar", fila.Estado);
    }

    [Fact]
    public async Task RechazaSemanaEnCurso_SemanaMuyAntigua_YFechaDesdePosterior()
    {
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(Historial()), new ModeloEspia(), Administradora());
        await Assert.ThrowsAsync<ArgumentException>(() => caso.EjecutarAsync(false, DateTime.Today, DesdeAmplio, true));
        await Assert.ThrowsAsync<ArgumentException>(() => caso.EjecutarAsync(false,
            SemanaPasada.AddDays(-7 * GenerarPrediccionDemandaUseCase.SemanasConsultables), DesdeAmplio, true));
        await Assert.ThrowsAsync<ArgumentException>(() => caso.EjecutarAsync(false, SemanaPasada, SemanaPasada.AddDays(1), true));
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(true, DateTime.Today, DateTime.Today, true));
    }

    [Fact]
    public async Task PermiteCancelarSinMostrarErrorDeModelo()
    {
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();
        var caso = new GenerarPrediccionDemandaUseCase(new Repositorio(), new ModeloEspia(), Administradora());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            caso.EjecutarAsync(true, DateTime.Today, DateTime.Today, true, cancelacion.Token));
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
        public decimal[] Salida { get; init; } = [0.25m, 0.25m, 0.25m, 0.25m, 0.25m, 0.25m, 0.25m];
        public decimal[] Predecir(IReadOnlyList<decimal> cantidades, int horizonte)
        {
            Assert.Equal(7, horizonte);
            Entrenamientos.Add(cantidades.ToArray());
            return Salida;
        }
    }
}
