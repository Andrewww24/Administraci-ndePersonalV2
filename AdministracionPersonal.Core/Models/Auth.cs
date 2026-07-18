namespace AdministracionPersonal.Core.Models;

/// <summary>Core4 — Credenciales que recibe el servicio de autenticación.</summary>
public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Core4 — Respuesta del servicio de autenticación cuando las credenciales son válidas.</summary>
public class LoginResponse
{
    public int IdUsuario { get; set; }
    public string Token { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
}

/// <summary>
/// Proyección interna de la tabla usuario para autenticación.
/// No se expone en la API: nunca se retorna en un endpoint porque contiene el password_hash.
/// </summary>
internal class UsuarioAuth
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int IntentosLogin { get; set; }
}
