namespace AdministracionPersonal.Core.Services;

/// <summary>
/// Construye un HttpClient apuntando a los Web Services del propio sistema Core,
/// para que las pantallas internas (Razor Pages) los consuman sin duplicar código.
/// </summary>
public interface ICoreApiClientFactory
{
    HttpClient CrearCliente();
}
