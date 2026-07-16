using Microsoft.AspNetCore.Mvc;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core4 — Autenticación de usuarios.
/// Responsable: Kendall (Core4, Core5)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IBitacoraService bitacora, ILogger<AuthController> logger)
    {
        _bitacora = bitacora;
        _logger = logger;
    }

    /// <summary>
    /// Core4 — Autentica un usuario con usuario y contraseña.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <returns>Token JWT si las credenciales son válidas.</returns>
    /// <response code="200">Autenticación exitosa. Retorna el token JWT.</response>
    /// <response code="400">Credenciales vacías o inválidas.</response>
    /// <response code="401">Usuario o contraseña incorrectos, o cuenta bloqueada.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // TODO Core4: Implementar según criterios de aceptación.
        // 1. Validar que usuario y contraseña no estén vacíos.
        // 2. Buscar el usuario en tabla usuario (estado = ACTIVO).
        // 3. Verificar la contraseña con el esquema AES-GCM del Alcance 1
        //    (formato: "AESGCM:iv:tag:cipherdata" en base64, separado por ':').
        //    Reutilizar o replicar la clase de cifrado del Alcance 1.
        // 4. Si la contraseña es incorrecta: incrementar intentos_login.
        //    Si llega a 3 intentos: cambiar estado a BLOQUEADO.
        // 5. Si es válida: resetear intentos_login, actualizar fecha_ultimo_login.
        // 6. Generar un JWT con los claims: id_usuario, usuario, nombre_completo.
        //    Usar la configuración Jwt:Key, Jwt:Issuer, Jwt:Audience, Jwt:ExpiresInMinutes.
        // 7. Registrar en bitácora: tipo=SELECT, entidad="usuario",
        //    descripcion=$"Login {(exitoso ? "exitoso" : "fallido")} para usuario {request.Usuario}".
        // 8. Retornar Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse { Token = token })).
        throw new NotImplementedException("Core4: Login pendiente de implementar.");
    }
}

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
}
