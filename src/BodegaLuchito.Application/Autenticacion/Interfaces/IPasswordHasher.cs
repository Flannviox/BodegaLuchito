namespace BodegaLuchito.Application.Autenticacion.Interfaces;

public interface IPasswordHasher
{
    string GenerarHash(string password);

    bool Verificar(
        string password,
        string passwordHash);
}
