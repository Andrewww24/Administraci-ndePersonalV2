using Microsoft.AspNetCore.Mvc;
using Dapper;
using AdministracionPersonal.Core.Models;
using AdministracionPersonal.Core.Repositories;
using AdministracionPersonal.Core.Services;

namespace AdministracionPersonal.Core.Controllers;

/// <summary>
/// Core2 y Core8 — Gestión de oferentes.
/// Responsable: Andrew (Core2, Core7, Core8)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OferentesController : ControllerBase
{
    private readonly IBitacoraService _bitacora;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<OferentesController> _logger;

    public OferentesController(IBitacoraService bitacora, IDbConnectionFactory dbFactory, ILogger<OferentesController> logger)
    {
        _bitacora = bitacora;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Core2 — Lista los oferentes aptos para un puesto específico.
    /// </summary>
    /// <param name="codigo">Código del puesto a consultar (columna `codigo` de la tabla `puesto`).</param>
    /// <returns>Lista de oferentes aptos para el puesto, con identificación y nombre.</returns>
    /// <response code="200">Lista obtenida correctamente.</response>
    /// <response code="400">Código de puesto inválido.</response>
    /// <response code="404">No existe el puesto, o no tiene oferentes aptos.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("aptos/{codigo}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OferenteAptoDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerOferentesAptos(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(ApiResponse<object>.Fail("El código de puesto es requerido."));

        try
        {
            using var connection = _dbFactory.CreateConnection();

            var idPuesto = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT id_puesto FROM puesto WHERE codigo = @Codigo",
                new { Codigo = codigo });

            if (idPuesto is null)
                return NotFound(ApiResponse<object>.Fail($"No existe un puesto con código {codigo}."));

            var oferentes = await connection.QueryAsync<OferenteAptoDto>(
                @"SELECT id_oferente AS IdOferente, identificacion AS Identificacion, nombre_completo AS NombreCompleto
                  FROM vw_oferentes_aptos_puesto
                  WHERE id_puesto = @IdPuesto",
                new { IdPuesto = idPuesto });

            var resultado = oferentes.ToList();
            if (resultado.Count == 0)
                return NotFound(ApiResponse<object>.Fail($"No hay oferentes aptos para el puesto {codigo}."));

            await _bitacora.RegistrarAsync("SELECT", "oferente", $"El usuario consulta oferentes aptos para puesto {codigo}");

            return Ok(ApiResponse<IEnumerable<OferenteAptoDto>>.Ok(resultado));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener oferentes aptos para puesto {Codigo}", codigo);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Core8 — Obtiene el detalle completo de un oferente a partir de su identificación.
    /// </summary>
    /// <param name="identificacion">Identificación del oferente (cédula, DIMEX o pasaporte).</param>
    /// <returns>Detalle completo del oferente.</returns>
    /// <response code="200">Detalle obtenido correctamente.</response>
    /// <response code="400">Identificación inválida.</response>
    /// <response code="404">Oferente no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{identificacion}")]
    [ProducesResponseType(typeof(ApiResponse<OferenteDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ObtenerDetalleOferente(string identificacion)
    {
        if (string.IsNullOrWhiteSpace(identificacion))
            return BadRequest(ApiResponse<object>.Fail("La identificación del oferente es requerida."));

        try
        {
            using var connection = _dbFactory.CreateConnection();

            var idOferente = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT id_oferente FROM oferente WHERE identificacion = @Identificacion",
                new { Identificacion = identificacion });

            if (idOferente is null)
                return NotFound(ApiResponse<object>.Fail($"No existe un oferente con identificación {identificacion}."));

            var detalle = await connection.QueryFirstOrDefaultAsync<OferenteDto>(
                @"SELECT id_oferente AS IdOferente, identificacion AS Identificacion,
                         tipo_identificacion AS TipoIdentificacion, nombre_completo AS NombreCompleto,
                         fecha_nacimiento AS FechaNacimiento, direccion AS Direccion,
                         id_distrito AS IdDistrito, fecha_registro AS FechaRegistro,
                         distrito AS Distrito, canton AS Canton, provincia AS Provincia,
                         correos AS Correos, telefonos AS Telefonos,
                         puestos_postulados AS PuestosPostulados, ruta_curriculum AS RutaCurriculum
                  FROM vw_detalle_oferente
                  WHERE id_oferente = @IdOferente",
                new { IdOferente = idOferente });

            if (detalle is null)
                return NotFound(ApiResponse<object>.Fail($"No existe un oferente con identificación {identificacion}."));

            await _bitacora.RegistrarAsync("SELECT", "oferente", $"El usuario consulta detalle del oferente {identificacion}");

            return Ok(ApiResponse<OferenteDto>.Ok(detalle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalle del oferente con identificación {Identificacion}", identificacion);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
        }
    }
}
