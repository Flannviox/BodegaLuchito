using BodegaLuchito.Infrastructure.Autenticacion.Security;

namespace BodegaLuchito.Tests.Infrastructure.Autenticacion;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Verificar_ConPasswordCorrecto_DebeRetornarTrue()
    {
        var hasher =
            new Pbkdf2PasswordHasher();

        const string password = "ClaveSegura123";

        var hash =
            hasher.GenerarHash(password);

        var resultado =
            hasher.Verificar(password, hash);

        Assert.True(resultado);
    }

    [Fact]
    public void Verificar_ConPasswordIncorrecto_DebeRetornarFalse()
    {
        var hasher =
            new Pbkdf2PasswordHasher();

        var hash =
            hasher.GenerarHash("ClaveCorrecta123");

        var resultado =
            hasher.Verificar(
                "ClaveIncorrecta",
                hash);

        Assert.False(resultado);
    }

    [Fact]
    public void GenerarHash_MismoPassword_DebeGenerarHashesDistintos()
    {
        var hasher =
            new Pbkdf2PasswordHasher();

        const string password = "ClaveSegura123";

        var hashUno =
            hasher.GenerarHash(password);

        var hashDos =
            hasher.GenerarHash(password);

        Assert.NotEqual(
            hashUno,
            hashDos);
    }
}
