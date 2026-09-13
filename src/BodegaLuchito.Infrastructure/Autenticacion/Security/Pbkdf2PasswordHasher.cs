using System.Security.Cryptography;
using BodegaLuchito.Application.Autenticacion.Interfaces;

namespace BodegaLuchito.Infrastructure.Autenticacion.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public string GenerarHash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt =
            RandomNumberGenerator.GetBytes(SaltSize);

        var hash =
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

        return string.Join(
            '$',
            "PBKDF2-SHA256",
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verificar(
        string password,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var partes = passwordHash.Split('$');

        if (partes.Length != 4)
        {
            return false;
        }

        if (partes[0] != "PBKDF2-SHA256")
        {
            return false;
        }

        if (!int.TryParse(
                partes[1],
                out var iterations))
        {
            return false;
        }

        try
        {
            var salt =
                Convert.FromBase64String(partes[2]);

            var hashEsperado =
                Convert.FromBase64String(partes[3]);

            var hashActual =
                Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(
                hashActual,
                hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
