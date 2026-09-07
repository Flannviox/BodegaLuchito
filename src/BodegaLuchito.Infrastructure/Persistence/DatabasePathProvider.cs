namespace BodegaLuchito.Infrastructure.Persistence;

public static class DatabasePathProvider
{
    public static string GetDatabasePath()
    {
        var localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        var databaseDirectory =
            Path.Combine(
                localAppData,
                "BodegaLuchito",
                "Data");

        Directory.CreateDirectory(databaseDirectory);

        return Path.Combine(
            databaseDirectory,
            "bodega_luchito.db");
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }
}
