using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core4 — Autenticación de usuarios.
/// Responsable original: Kendall (Core4, Core5).
/// Implementado por Fran a solicitud del equipo para poder probar de punta a punta
/// los flujos de puestos/oferentes/empleados que dependen de un login funcional.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private const int MaxIntentosLogin = 3;

    private readonly IDbConnectionFactory _dbFactory;
    private readonly IBitacoraService _bitacora;
    private readonly IPasswordEncryptionService _passwordEncryption;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IDbConnectionFactory dbFactory,
        IBitacoraService bitacora,
        IPasswordEncryptionService passwordEncryption,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _dbFactory = dbFactory;
        _bitacora = bitacora;
        _passwordEncryption = passwordEncryption;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Core4 — Autentica un usuario con usuario y contraseña.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <returns>Token JWT si las credenciales son válidas.</returns>
    /// <response code="200">Autenticación exitosa. Retorna el token JWT.</response>
    /// <response code="400">Credenciales vacías o inválidas.</response>
    /// <response code="401">Usuario o contraseña incorrectos, o cuenta bloqueada/inactiva.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<object>.Fail("Usuario y contraseña son requeridos."));
        }

        try
        {
            using var connection = _dbFactory.CreateConnection();

            var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioAuth>(
                @"SELECT id_usuario AS IdUsuario, usuario AS Usuario, nombre_completo AS NombreCompleto,
                         password_hash AS PasswordHash, estado AS Estado, intentos_login AS IntentosLogin
                  FROM usuario
                  WHERE usuario = @Usuario",
                new { request.Usuario });

            if (usuario is null)
            {
                await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login fallido para usuario {request.Usuario}");
                return Unauthorized(ApiResponse<object>.Fail("Usuario y/o contraseña incorrectos."));
            }

            if (usuario.Estado == "BLOQUEADO")
            {
                await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login fallido para usuario {request.Usuario} (cuenta bloqueada)", usuario.IdUsuario);
                return Unauthorized(ApiResponse<object>.Fail("El usuario se encuentra bloqueado."));
            }

            if (usuario.Estado == "INACTIVO")
            {
                await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login fallido para usuario {request.Usuario} (cuenta inactiva)", usuario.IdUsuario);
                return Unauthorized(ApiResponse<object>.Fail("El usuario se encuentra inactivo."));
            }

            var credencialesValidas = _passwordEncryption.Verify(request.Password, usuario.PasswordHash);

            if (!credencialesValidas)
            {
                var intentos = usuario.IntentosLogin + 1;

                if (intentos >= MaxIntentosLogin)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE usuario
                          SET intentos_login = @Intentos, estado = 'BLOQUEADO', fecha_bloqueo = @Ahora
                          WHERE id_usuario = @IdUsuario",
                        new { Intentos = intentos, Ahora = DateTime.Now, usuario.IdUsuario });
                }
                else
                {
                    await connection.ExecuteAsync(
                        "UPDATE usuario SET intentos_login = @Intentos WHERE id_usuario = @IdUsuario",
                        new { Intentos = intentos, usuario.IdUsuario });
                }

                await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login fallido para usuario {request.Usuario}", usuario.IdUsuario);
                return Unauthorized(ApiResponse<object>.Fail("Usuario y/o contraseña incorrectos."));
            }

            await connection.ExecuteAsync(
                "UPDATE usuario SET intentos_login = 0, fecha_ultimo_login = @Ahora WHERE id_usuario = @IdUsuario",
                new { Ahora = DateTime.Now, usuario.IdUsuario });

            var expiraEnMinutos = _configuration.GetValue<int>("Jwt:ExpiresInMinutes");
            var token = GenerarToken(usuario, expiraEnMinutos);

            await _bitacora.RegistrarAsync("SELECT", "usuario", $"Login exitoso para usuario {request.Usuario}", usuario.IdUsuario);

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                NombreCompleto = usuario.NombreCompleto,
                Expira = DateTime.UtcNow.AddMinutes(expiraEnMinutos)
            }, "Autenticación exitosa."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al autenticar al usuario {Usuario}", request.Usuario);
            return StatusCode(500, ApiResponse<object>.Fail("Ocurrió un error al autenticar al usuario."));
        }
    }

    /// <summary>
    /// Utilidad de DESARROLLO (deshabilitada fuera de Development): genera un hash
    /// AES-GCM compatible con usuario.password_hash a partir de una contraseña en texto
    /// plano, para poder crear o actualizar usuarios de prueba con
    /// "UPDATE usuario SET password_hash = '&lt;hash&gt;' WHERE usuario = '...'" sin
    /// depender de la clave original con la que el Alcance 1 cifró los usuarios existentes.
    /// </summary>
    /// <param name="request">Contraseña en texto plano a cifrar.</param>
    [HttpPost("generar-hash-prueba")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(404)]
    public IActionResult GenerarHashDePrueba([FromBody] GenerarHashRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var hash = _passwordEncryption.Encrypt(request.Password);
        return Ok(ApiResponse<string>.Ok(hash, "Hash de prueba generado. Úselo únicamente para datos de prueba."));
    }

    private string GenerarToken(UsuarioAuth usuario, int expiraEnMinutos)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurado en appsettings.");

        var claims = new[]
        {
            new Claim("id_usuario", usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Usuario),
            new Claim("nombre_completo", usuario.NombreCompleto)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiraEnMinutos),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

public class GenerarHashRequest
{
    public string Password { get; set; } = string.Empty;
}

internal class UsuarioAuth
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int IntentosLogin { get; set; }
}
