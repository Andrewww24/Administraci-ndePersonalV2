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
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IBitacoraService _bitacora;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IPasswordCryptoService _crypto;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    private const int MaxIntentos = 3;

    // Mensaje único exigido por el criterio de aceptación (Core5 / Seg1):
    // aplica tanto a campos vacíos como a credenciales incorrectas.
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
    /// <response code="401">Credenciales vacías, incorrectas, o cuenta bloqueada/inactiva.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Campos vacíos: el criterio pide el MISMO mensaje que credenciales inválidas.
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Usuario) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(ApiResponse<object>.Fail(MsgCredenciales));
        }

        try
        {
            using var connection = _dbFactory.CreateConnection();

            // 2. Buscar el usuario SIN filtrar por estado, para poder distinguir
            //    "no existe" de "existe pero está bloqueado/inactivo" en la bitácora.
            var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioAuth>(
                @"SELECT id_usuario      AS IdUsuario,
                         usuario         AS Usuario,
                         nombre_completo AS NombreCompleto,
                         password_hash   AS PasswordHash,
                         estado          AS Estado,
                         intentos_login  AS IntentosLogin
                  FROM usuario
                  WHERE usuario = @Usuario",
                new { request.Usuario });

            if (usuario is null)
            {
                await _bitacora.RegistrarAsync(
                    "ERROR", "usuario", $"Login fallido: el usuario '{request.Usuario}' no existe.");
                return Unauthorized(ApiResponse<object>.Fail(MsgCredenciales));
            }

            // 3. Usuario no habilitado: no se cuentan intentos ni se valida la contraseña.
            if (!string.Equals(usuario.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase))
            {
                await _bitacora.RegistrarAsync(
                    "ERROR", "usuario",
                    $"Login rechazado: el usuario '{usuario.Usuario}' está en estado {usuario.Estado}.",
                    usuario.IdUsuario);
                return Unauthorized(ApiResponse<object>.Fail(MsgCredenciales));
            }

            // 4. Verificar la contraseña con el mismo esquema AES-GCM del Alcance 1.
            if (!_crypto.Verificar(request.Password, usuario.PasswordHash))
            {
                var intentos = usuario.IntentosLogin + 1;
                var bloquear = intentos >= MaxIntentos;

                if (bloquear)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE usuario
                          SET intentos_login = @Intentos, estado = 'BLOQUEADO', fecha_bloqueo = @Ahora
                          WHERE id_usuario = @Id",
                        new { Intentos = intentos, Ahora = DateTime.Now, Id = usuario.IdUsuario });

                    await _bitacora.RegistrarAsync(
                        "UPDATE", "usuario",
                        $"Usuario '{usuario.Usuario}' bloqueado por {MaxIntentos} intentos de login fallidos.",
                        usuario.IdUsuario,
                        datosAnteriores: new { estado = usuario.Estado, intentos_login = usuario.IntentosLogin },
                        datosNuevos: new { estado = "BLOQUEADO", intentos_login = intentos });
                }
                else
                {
                    await connection.ExecuteAsync(
                        "UPDATE usuario SET intentos_login = @Intentos WHERE id_usuario = @Id",
                        new { Intentos = intentos, Id = usuario.IdUsuario });

                    await _bitacora.RegistrarAsync(
                        "ERROR", "usuario",
                        $"Login fallido para '{usuario.Usuario}'. Intento {intentos} de {MaxIntentos}.",
                        usuario.IdUsuario);
                }

                return Unauthorized(ApiResponse<object>.Fail(MsgCredenciales));
            }

            // 5. Credenciales válidas: resetear intentos y registrar el último ingreso.
            await connection.ExecuteAsync(
                "UPDATE usuario SET intentos_login = 0, fecha_ultimo_login = @Ahora WHERE id_usuario = @Id",
                new { Ahora = DateTime.Now, Id = usuario.IdUsuario });

            // 6. Generar el JWT.
            var (token, expira) = GenerarToken(usuario);

            await _bitacora.RegistrarAsync(
                "SELECT", "usuario", $"Login exitoso para el usuario '{usuario.Usuario}'.", usuario.IdUsuario);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                IdUsuario = usuario.IdUsuario,
                Token = token,
                NombreCompleto = usuario.NombreCompleto,
                Expira = expira
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login para el usuario {Usuario}", request.Usuario);
            await _bitacora.RegistrarAsync("ERROR", "usuario", $"Error técnico en el login: {ex.Message}");
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
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
