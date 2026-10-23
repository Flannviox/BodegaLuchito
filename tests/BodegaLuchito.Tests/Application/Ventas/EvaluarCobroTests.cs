using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Tests.Application.Ventas;

public class EvaluarCobroTests
{
    [Theory]
    [InlineData("20", 7.50)]
    [InlineData("12.50", 0)]
    [InlineData("12,50", 0)]
    [InlineData(" 15.00 ", 2.50)]
    public void EfectivoCalculaVuelto(string recibido, double esperado)
    {
        var r = EvaluarCobroUseCase.Ejecutar(12.50m, MetodoPago.Efectivo, recibido);
        Assert.True(r.Valido);
        Assert.Equal((decimal)esperado, r.Vuelto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12.49")]
    [InlineData("0")]
    [InlineData("-20")]
    [InlineData("abc")]
    [InlineData("1,000.00")]
    [InlineData("12.500")]
    [InlineData("20.")]
    [InlineData("9999999999999999999999999999999999")]
    public void BloqueaMontoInvalidoOInsuficiente(string recibido)
    {
        var r = EvaluarCobroUseCase.Ejecutar(12.50m, MetodoPago.Efectivo, recibido);
        Assert.False(r.Valido);
        Assert.Equal(0, r.Vuelto);
    }

    [Theory]
    [InlineData(MetodoPago.Yape)]
    [InlineData(MetodoPago.Plin)]
    public void PagoDigitalSiempreEsExacto(MetodoPago medio)
    {
        var r = EvaluarCobroUseCase.Ejecutar(12.50m, medio, "100");
        Assert.True(r.Valido);
        Assert.Equal(12.50m, r.Recibido);
        Assert.Equal(0, r.Vuelto);
    }

    [Fact]
    public void RechazaVentaSinTotalOMedioValido()
    {
        Assert.False(EvaluarCobroUseCase.Ejecutar(0, MetodoPago.Efectivo, "20").Valido);
        Assert.False(EvaluarCobroUseCase.Ejecutar(12.501m, MetodoPago.Yape, "").Valido);
        Assert.False(EvaluarCobroUseCase.Ejecutar(12.50m, (MetodoPago)99, "20").Valido);
    }
}
