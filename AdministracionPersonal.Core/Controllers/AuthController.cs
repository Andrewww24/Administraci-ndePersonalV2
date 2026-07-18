using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
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
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IPasswordCryptoService _crypto;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    private const int MaxIntentos = 3;
    private const string MsgCredenciales = "Usuario y/o contraseña incorrectos.";

    public AuthController(
        IBitacoraService bitacora,
        IDbConnectionFactory dbFactory,
        IPasswordCryptoService crypto,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _bitacora = bitacora;
        _dbFactory = dbFactory;
        _crypto = crypto;
        _config = config;
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
        // 1. Validar que usuario y contraseña no estén vacíos.
        if (request is null || string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<object>.Fail("El usuario y la contraseña son requeridos."));

        try
        {
            using var connection = _dbFactory.CreateConnection();

            // 2. Buscar el usuario en tabla usuario (estado = ACTIVO).
            var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioAuth>(
                @"SELECT id_usuario AS IdUsuario, usuario AS Usuario, nombre_completo AS NombreCompleto,
                         password_hash AS PasswordHash, intentos_login AS IntentosLogin
                  FROM usuario
                  WHERE usuario = @Usuario AND estado = 'ACTIVO'",
                new { request.Usuario });

            if (usuario is null)
            {
                await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login fallido para usuario {request.Usuario}");
                return Unauthorized(ApiResponse<object>.Fail(MsgCredenciales));
            }

            // 3. Verificar la contraseña con el esquema AES-GCM del Alcance 1.
            var passwordValida = _crypto.Verificar(request.Password, usuario.PasswordHash);

            if (!passwordValida)
            {
                // 4. Contraseña incorrecta: incrementar intentos_login; a los 3, bloquear.
                var intentos = usuario.IntentosLogin + 1;
                if (intentos >= MaxIntentos)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE usuario SET intentos_login = @Intentos, estado = 'BLOQUEADO', fecha_bloqueo = @Ahora
                          WHERE id_usuario = @Id",
                        new { Intentos = intentos, Ahora = DateTime.Now, Id = usuario.IdUsuario });
                }
                else
                {
                    await connection.ExecuteAsync(
                        "UPDATE usuario SET intentos_login = @Intentos WHERE id_usuario = @Id",
                        new { Intentos = intentos, Id = usuario.IdUsuario });
                }

                await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login fallido para usuario {usuario.Usuario}");
                return Unauthorized(ApiResponse<object>.Fail(MsgCredenciales));
            }

            // 5. Credenciales válidas: resetear intentos y actualizar fecha_ultimo_login.
            await connection.ExecuteAsync(
                "UPDATE usuario SET intentos_login = 0, fecha_ultimo_login = @Ahora WHERE id_usuario = @Id",
                new { Ahora = DateTime.Now, Id = usuario.IdUsuario });

            // 6. Generar JWT con claims id_usuario, usuario, nombre_completo.
            var (token, expira) = GenerarToken(usuario);

            // 7. Bitácora del login exitoso.
            await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login exitoso para usuario {usuario.Usuario}");

            // 8. Retornar el token.
            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                NombreCompleto = usuario.NombreCompleto,
                Expira = expira
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login para usuario {Usuario}", request.Usuario);
            return StatusCode(500, ApiResponse<object>.Fail("Ocurrió un error al procesar la autenticación."));
        }
    }

    private (string Token, DateTime Expira) GenerarToken(UsuarioAuth usuario)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado.");
        var minutos = int.TryParse(_config["Jwt:ExpiresInMinutes"], out var m) ? m : 60;
        var expira = DateTime.UtcNow.AddMinutes(minutos);

        var claims = new[]
        {
            new Claim("id_usuario", usuario.IdUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Usuario),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

/// <summary>Proyección interna de usuario para autenticación (no se expone en la API).</summary>
public class UsuarioAuth
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int IntentosLogin { get; set; }
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
